
using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Storage.Macros;

namespace Procedural.NET.Storage;

public sealed class GraphSessionService
{
	private readonly GraphFactory _factory;

	public GraphSessionService(GraphFactory factory)
	{
		_factory = factory;
	}

	public GraphSession Capture(GraphDocument document, string? currentPath)
	{
		var now = DateTime.UtcNow.ToString("O");
		
		var session = new GraphSession
		{
			Metadata =
			{
				Author = document.Metadata.Author,
				Description = document.Metadata.Description,
				Seed = document.Metadata.Seed,
				GeneratorVersion = document.Metadata.GeneratorVersion,
				AreaWidth = document.Metadata.AreaWidth,
				AreaHeight = document.Metadata.AreaHeight,
				OriginX = document.Metadata.OriginX,
				OriginY = document.Metadata.OriginY,
				AspectRatio = document.Metadata.AspectRatio,
				BuildResolutionPower = document.Metadata.BuildResolutionPower,
				BuildResolutionMode = document.Metadata.BuildResolutionMode,
				SeaLevel = document.Metadata.SeaLevel,
				ColorPreset = document.Metadata.ColorPreset,
				Units = document.Metadata.Units,
				RescalingMode = document.Metadata.RescalingMode,
				Name = string.IsNullOrWhiteSpace(currentPath)
					? "Untitled"
					: Path.GetFileNameWithoutExtension(currentPath),
				ModifiedUtc = now
			}
		};

		foreach (var node in document.Nodes)
			session.Nodes.Add(CaptureNode(node));

		foreach (var (input, output) in document.GetAllConnections())
		{
			session.Connections.Add(new SessionConnection
			{
				InputNodeId = input.NodeId,
				InputPortKey = input.PortKey,
				OutputNodeId = output.NodeId,
				OutputPortKey = output.PortKey
			});
		}

		return session;
	}

	public GraphSession CaptureSelection(GraphDocument document, IReadOnlyCollection<GraphNode> selection, string? name = null)
	{
		var selectedIds = selection.Select(static node => node.Id).ToHashSet();
		var session = new GraphSession
		{
			Metadata =
			{
				Author = document.Metadata.Author,
				Description = document.Metadata.Description,
				Seed = document.Metadata.Seed,
				GeneratorVersion = document.Metadata.GeneratorVersion,
				AreaWidth = document.Metadata.AreaWidth,
				AreaHeight = document.Metadata.AreaHeight,
				OriginX = document.Metadata.OriginX,
				OriginY = document.Metadata.OriginY,
				AspectRatio = document.Metadata.AspectRatio,
				BuildResolutionPower = document.Metadata.BuildResolutionPower,
				BuildResolutionMode = document.Metadata.BuildResolutionMode,
				SeaLevel = document.Metadata.SeaLevel,
				ColorPreset = document.Metadata.ColorPreset,
				Units = document.Metadata.Units,
				RescalingMode = document.Metadata.RescalingMode,
				Name = string.IsNullOrWhiteSpace(name) ? "Macro" : name,
				ModifiedUtc = DateTime.UtcNow.ToString("O")
			}
		};

		foreach (var node in document.Nodes.Where(node => selectedIds.Contains(node.Id)))
			session.Nodes.Add(CaptureNode(node));

		foreach (var (input, output) in document.GetAllConnections())
		{
			if (!selectedIds.Contains(input.NodeId) || !selectedIds.Contains(output.NodeId))
				continue;

			session.Connections.Add(new SessionConnection
			{
				InputNodeId = input.NodeId,
				InputPortKey = input.PortKey,
				OutputNodeId = output.NodeId,
				OutputPortKey = output.PortKey
			});
		}

		MacroSessionMetadataBuilder.Apply(session);
		return session;
	}

	public IReadOnlyList<GraphNode> Apply(GraphDocument document, GraphSession session)
	{
		document.Clear();
		document.Metadata = CloneMetadata(session.Metadata);
		var loaded = new List<GraphNode>(session.Nodes.Count);

		foreach (var saved in session.Nodes)
		{
			var model = _factory.CreateNode(saved.ExecutorKey, saved.Id, new Float2(saved.PositionX, saved.PositionY));
			RestoreNode(model, saved);
			document.AddNode(model);
			loaded.Add(model);
		}

		foreach (var connection in session.Connections)
		{
			document.TryConnect(
				connection.OutputNodeId,
				connection.OutputPortKey,
				connection.InputNodeId,
				connection.InputPortKey);
		}

		document.ClearSelection();
		return loaded;
	}

	public IReadOnlyList<GraphNode> Append(GraphDocument document, GraphSession session, Float2 targetOrigin)
	{
		if (session.Nodes.Count == 0)
			return [];

		var minX = session.Nodes.Min(static node => node.PositionX);
		var minY = session.Nodes.Min(static node => node.PositionY);
		var idMap = new Dictionary<Guid, Guid>(session.Nodes.Count);
		var loaded = new List<GraphNode>(session.Nodes.Count);

		foreach (var saved in session.Nodes)
		{
			var position = new Float2(
				targetOrigin.X + (saved.PositionX - minX),
				targetOrigin.Y + (saved.PositionY - minY));

			var model = _factory.CreateNode(saved.ExecutorKey, position);
			RestoreNode(model, saved);
			document.AddNode(model);
			idMap[saved.Id] = model.Id;
			loaded.Add(model);
		}

		foreach (var connection in session.Connections)
		{
			if (!idMap.TryGetValue(connection.OutputNodeId, out var outputId) ||
			    !idMap.TryGetValue(connection.InputNodeId, out var inputId))
				continue;

			document.TryConnect(outputId, connection.OutputPortKey, inputId, connection.InputPortKey);
		}

		document.SetSelection(loaded);
		return loaded;
	}

	public SessionValidationResult Validate(GraphSession session)
	{
		var worldExportIds = GetWorldExportIds(session);

		if (worldExportIds.Length == 0)
			return SessionValidationResult.Valid([]);

		if (worldExportIds.Length > 1)
			return SessionValidationResult.Invalid(["More than one WorldExportNode was found. Keep exactly one export graphNode."]);

		return ValidateWorldExport(session, worldExportIds[0]);
	}

	public SessionValidationResult ValidateForWorldExport(GraphSession session)
	{
		var worldExportIds = GetWorldExportIds(session);

		if (worldExportIds.Length == 0)
			return SessionValidationResult.Invalid(["WorldExportNode is missing."]);

		if (worldExportIds.Length > 1)
			return SessionValidationResult.Invalid(["More than one WorldExportNode was found. Keep exactly one export graphNode."]);

		return ValidateWorldExport(session, worldExportIds[0]);
	}

	private static Guid[] GetWorldExportIds(GraphSession session) =>
	[
		.. session.Nodes
		          .Where(static node => string.Equals(node.ExecutorKey, NodeKeys.WorldExport, StringComparison.Ordinal))
		          .Select(static node => node.Id)
	];

	private static SessionValidationResult ValidateWorldExport(GraphSession session, Guid worldExportId)
	{
		var errors = new List<string>();
		var warnings = new List<string>();

		if (!HasInputConnection(session, worldExportId, PortKeys.C))
			errors.Add("WorldExportNode elevation input is not connected.");

		if (!HasAnyInputConnection(session, worldExportId, PortKeys.WaterPrefix))
			warnings.Add("WorldExportNode has no water inputs connected.");

		if (!HasAnyInputConnection(session, worldExportId, PortKeys.ScatterPrefix))
			warnings.Add("WorldExportNode has no scatter inputs connected.");

		return errors.Count == 0
			? SessionValidationResult.Valid(warnings)
			: SessionValidationResult.Invalid(errors, warnings);
	}

	private static bool HasInputConnection(GraphSession session, Guid inputNodeId, string inputPortKey) =>
		session.Connections.Any(connection =>
			connection.InputNodeId == inputNodeId &&
			string.Equals(connection.InputPortKey, inputPortKey, StringComparison.Ordinal));

	private static bool HasAnyInputConnection(GraphSession session, Guid inputNodeId, string inputPortPrefix) =>
		session.Connections.Any(connection =>
			connection.InputNodeId == inputNodeId &&
			connection.InputPortKey.StartsWith(inputPortPrefix, StringComparison.Ordinal));

	private static SessionNode CaptureNode(GraphNode graphNode) => new()
	{
		Id = graphNode.Id,
		ExecutorKey = graphNode.Executor.Key,
		CustomName = graphNode.CustomName,
		PositionX = graphNode.Position.X,
		PositionY = graphNode.Position.Y,
		SizeX = graphNode.Size.X,
		SizeY = graphNode.Size.Y,
		Parameters = new Dictionary<string, float>(graphNode.Parameters.Parameters, StringComparer.Ordinal),
		StringParameters = new Dictionary<string, string>(graphNode.Parameters.StringParameters, StringComparer.Ordinal),
		PinnedParameters = [.. graphNode.Parameters.PinnedParameters]
	};

	private static void RestoreNode(GraphNode model, SessionNode saved)
	{
		model.CustomName = saved.CustomName;
		if (saved is { SizeX: > 0, SizeY: > 0 })
			model.Size = new Float2(saved.SizeX, saved.SizeY);

		foreach (var (key, value) in saved.StringParameters)
			model.Parameters.SetString(key, value);

		model.Executor.UpdatePorts(model);

		foreach (var (key, value) in saved.Parameters)
		{
			if (model.Parameters.Parameters.ContainsKey(key))
				model.Parameters.Set(key, value);
		}

		foreach (var key in saved.PinnedParameters)
		{
			if (model.Parameters.Parameters.ContainsKey(key))
				model.Parameters.SetParameterPinned(key, true);
		}

		model.Executor.UpdatePorts(model);
	}

	private static GraphMetadata CloneMetadata(GraphMetadata metadata)
	{
		return new GraphMetadata
		{
			Name = metadata.Name,
			Description = metadata.Description,
			Author = metadata.Author,
			CreatedUtc = metadata.CreatedUtc,
			ModifiedUtc = metadata.ModifiedUtc,
			Seed = metadata.Seed,
			GeneratorVersion = metadata.GeneratorVersion,
			AreaWidth = metadata.AreaWidth,
			AreaHeight = metadata.AreaHeight,
			OriginX = metadata.OriginX,
			OriginY = metadata.OriginY,
			AspectRatio = metadata.AspectRatio,
			BuildResolutionPower = metadata.BuildResolutionPower,
			BuildResolutionMode = metadata.BuildResolutionMode,
			SeaLevel = metadata.SeaLevel,
			ColorPreset = metadata.ColorPreset,
			Units = metadata.Units,
			RescalingMode = metadata.RescalingMode,
			Tags = new Dictionary<string, string>(metadata.Tags, StringComparer.OrdinalIgnoreCase)
		};
	}
}
