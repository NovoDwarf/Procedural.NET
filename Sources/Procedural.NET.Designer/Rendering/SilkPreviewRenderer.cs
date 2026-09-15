using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Procedural.NET.Designer.Rendering;

internal sealed unsafe class SilkPreviewRenderer : IPreviewRenderer
{
	private const int MeshSize = 128;
	private const int RenderScale = 2;

	private readonly IPreviewRenderer _fallback;
	private readonly Dictionary<string, PreviewFrame> _cache = [];
	private readonly Lock _gate = new();
	
	private Glfw? _glfw;
	private GL? _gl;
	private WindowHandle* _window;
	
	private uint _program;
	private uint _vao;
	private uint _vbo;
	private uint _ebo;
	private uint _texture;
	private uint _framebuffer;
	private uint _colorTarget;
	private uint _depthTarget;
	private bool _failed;

	public SilkPreviewRenderer(IPreviewRenderer fallback)
	{
		_fallback = fallback;
	}

	public string BackendName => "Silk.NET OpenGL";

	public PreviewFrame Render(PreviewRenderRequest request)
	{
		if (!request.IsThreeD)
			return _fallback.Render(request);

		var source = _fallback.Render(request with { IsThreeD = false });
		var key = $"{source.CacheKey}:silk3d:{request.Yaw:0.###}:{request.Pitch:0.###}:{request.Zoom:0.###}:{request.HighResolution}";
		if (_cache.TryGetValue(key, out var cached))
			return cached;

		lock (_gate)
		{
			if (_cache.TryGetValue(key, out cached))
				return cached;

			if (_failed)
				return source;

			try
			{
				EnsureContext();
				var frame = RenderTerrain(source, key, request);
				_cache[key] = frame;
				return frame;
			}
			catch
			{
				_failed = true;
				return source;
			}
		}
	}

	private void EnsureContext()
	{
		if (_gl is not null)
			return;

		_glfw = Glfw.GetApi();
		if (!_glfw.Init())
			throw new InvalidOperationException("GLFW initialization failed.");

		_glfw.WindowHint(WindowHintBool.Visible, false);
		_glfw.WindowHint(WindowHintBool.Resizable, false);
		_glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGL);
		_glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
		_glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
		_glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

		_window = _glfw.CreateWindow(RenderResolution, RenderResolution, "Procedural.NET Preview", null, null);
		if (_window is null)
			throw new InvalidOperationException("OpenGL preview window creation failed.");

		_glfw.MakeContextCurrent(_window);
		_gl = GL.GetApi(_glfw.GetProcAddress);

		CreateResources();
	}

	private PreviewFrame RenderTerrain(PreviewFrame source, string key, PreviewRenderRequest request)
	{
		var gl = _gl!;
		_glfw!.MakeContextCurrent(_window);

		var vertices = BuildVertices(source, request);
		var indices = BuildIndices();

		fixed (float* vertexPtr = vertices)
		fixed (uint* indexPtr = indices)
		fixed (byte* texturePtr = source.BgraPixels)
		{
			gl.BindVertexArray(_vao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), vertexPtr, BufferUsageARB.DynamicDraw);

			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
			gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indexPtr, BufferUsageARB.DynamicDraw);

			gl.BindTexture(TextureTarget.Texture2D, _texture);
			gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, PreviewFrame.Resolution, PreviewFrame.Resolution, 0, PixelFormat.Bgra, PixelType.UnsignedByte, texturePtr);
		}

		gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
		gl.Viewport(0, 0, (uint)RenderResolution, (uint)RenderResolution);
		gl.Enable(EnableCap.DepthTest);
		gl.DepthFunc(DepthFunction.Less);
		gl.ClearColor(0.047f, 0.055f, 0.071f, 1f);
		gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

		gl.UseProgram(_program);
		gl.BindVertexArray(_vao);
		gl.ActiveTexture(TextureUnit.Texture0);
		gl.BindTexture(TextureTarget.Texture2D, _texture);
		gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);

		var renderPixels = new byte[RenderResolution * RenderResolution * 4];
		fixed (byte* pixelPtr = renderPixels)
			gl.ReadPixels(0, 0, (uint)RenderResolution, (uint)RenderResolution, GLEnum.Bgra, GLEnum.UnsignedByte, pixelPtr);

		FlipRows(renderPixels, RenderResolution);
		var pixels = Downsample(renderPixels);
		return new PreviewFrame(key, pixels, source.HeightSamples);
	}

	private void CreateResources()
	{
		var gl = _gl!;
		_program = CreateProgram(gl);
		_vao = gl.GenVertexArray();
		_vbo = gl.GenBuffer();
		_ebo = gl.GenBuffer();
		_texture = gl.GenTexture();
		_framebuffer = gl.GenFramebuffer();
		_colorTarget = gl.GenTexture();
		_depthTarget = gl.GenRenderbuffer();

		gl.BindVertexArray(_vao);
		gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
		gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

		const uint stride = 5 * sizeof(float);
		gl.EnableVertexAttribArray(0);
		gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, null);
		gl.EnableVertexAttribArray(1);
		gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));

		gl.BindTexture(TextureTarget.Texture2D, _texture);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

		gl.BindTexture(TextureTarget.Texture2D, _colorTarget);
		gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)RenderResolution, (uint)RenderResolution, 0, GLEnum.Bgra, GLEnum.UnsignedByte, null);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

		gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthTarget);
		gl.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.DepthComponent24, (uint)RenderResolution, (uint)RenderResolution);

		gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
		gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTarget, 0);
		gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthTarget);
	}

	private static uint CreateProgram(GL gl)
	{
		var vertex = Compile(gl, ShaderType.VertexShader, """
			#version 330 core
			layout (location = 0) in vec3 aPosition;
			layout (location = 1) in vec2 aUv;
			out vec2 vUv;
			out vec3 vPosition;
			void main()
			{
				gl_Position = vec4(aPosition, 1.0);
				vUv = aUv;
				vPosition = aPosition;
			}
			""");

		var fragment = Compile(gl, ShaderType.FragmentShader, """
			#version 330 core
			in vec2 vUv;
			in vec3 vPosition;
			out vec4 FragColor;
			uniform sampler2D uTexture;
			void main()
			{
				vec4 color = texture(uTexture, vUv);
				vec3 dx = dFdx(vPosition);
				vec3 dy = dFdy(vPosition);
				vec3 normal = normalize(cross(dx, dy));
				vec3 lightDir = normalize(vec3(-0.38, 0.72, 0.58));
				float diffuse = max(abs(dot(normal, lightDir)), 0.0);
				float heightLight = clamp(0.92 + vPosition.y * 0.22, 0.78, 1.12);
				float light = (0.38 + diffuse * 0.62) * heightLight;
				FragColor = vec4(color.rgb * light, color.a);
			}
			""");

		var program = gl.CreateProgram();
		gl.AttachShader(program, vertex);
		gl.AttachShader(program, fragment);
		gl.LinkProgram(program);
		gl.DeleteShader(vertex);
		gl.DeleteShader(fragment);
		
		return program;
	}

	private static uint Compile(GL gl, ShaderType type, string source)
	{
		var shader = gl.CreateShader(type);
		gl.ShaderSource(shader, source);
		gl.CompileShader(shader);
		
		return shader;
	}

	private static float[] BuildVertices(PreviewFrame source, PreviewRenderRequest request)
	{
		var vertices = new float[MeshSize * MeshSize * 5];
		var offset = 0;
		for (var row = 0; row < MeshSize; row++)
		for (var column = 0; column < MeshSize; column++)
		{
			var u = column / (float)(MeshSize - 1);
			var v = row / (float)(MeshSize - 1);
			var height = source.SampleHeight(u, v);
			var point = Project(u - 0.5, height * 0.45, v - 0.5, request);
			vertices[offset++] = point.X;
			vertices[offset++] = point.Y;
			vertices[offset++] = point.Z;
			vertices[offset++] = u;
			vertices[offset++] = v;
		}

		return vertices;
	}

	private static uint[] BuildIndices()
	{
		var indices = new uint[(MeshSize - 1) * (MeshSize - 1) * 6];
		var offset = 0;
		for (var row = 0; row < MeshSize - 1; row++)
		for (var column = 0; column < MeshSize - 1; column++)
		{
			var a = (uint)(row * MeshSize + column);
			var b = a + 1;
			var c = a + MeshSize;
			var d = c + 1;
			indices[offset++] = a;
			indices[offset++] = b;
			indices[offset++] = d;
			indices[offset++] = a;
			indices[offset++] = d;
			indices[offset++] = c;
		}

		return indices;
	}

	private static PreviewVertex Project(double x, double y, double z, PreviewRenderRequest request)
	{
		var yawRad = request.Yaw * Math.PI / 180;
		var pitchRad = request.Pitch * Math.PI / 180;
		var cosYaw = Math.Cos(yawRad);
		var sinYaw = Math.Sin(yawRad);
		var cosPitch = Math.Cos(pitchRad);
		var sinPitch = Math.Sin(pitchRad);

		var rx = x * cosYaw - z * sinYaw;
		var rz = x * sinYaw + z * cosYaw;
		var ry = y * cosPitch - rz * sinPitch;
		var depth = y * sinPitch + rz * cosPitch;
		var scale = 1.45 * request.Zoom;

		return new PreviewVertex(
			(float)(rx * scale),
			(float)(ry * scale - 0.10),
			(float)(-depth * 0.5));
	}

	private static void FlipRows(byte[] pixels, int resolution)
	{
		var stride = resolution * 4;
		var buffer = new byte[stride];
		for (var top = 0; top < resolution / 2; top++)
		{
			var bottom = resolution - top - 1;
			System.Buffer.BlockCopy(pixels, top * stride, buffer, 0, stride);
			System.Buffer.BlockCopy(pixels, bottom * stride, pixels, top * stride, stride);
			System.Buffer.BlockCopy(buffer, 0, pixels, bottom * stride, stride);
		}
	}

	private static byte[] Downsample(byte[] source)
	{
		var target = new byte[PreviewFrame.Resolution * PreviewFrame.Resolution * 4];
		for (var y = 0; y < PreviewFrame.Resolution; y++)
		for (var x = 0; x < PreviewFrame.Resolution; x++)
		{
			var r = 0;
			var g = 0;
			var b = 0;
			var a = 0;
			for (var yy = 0; yy < RenderScale; yy++)
			for (var xx = 0; xx < RenderScale; xx++)
			{
				var sourceOffset = (((y * RenderScale + yy) * RenderResolution) + x * RenderScale + xx) * 4;
				b += source[sourceOffset];
				g += source[sourceOffset + 1];
				r += source[sourceOffset + 2];
				a += source[sourceOffset + 3];
			}

			var targetOffset = (y * PreviewFrame.Resolution + x) * 4;
			target[targetOffset] = (byte)(b / 4);
			target[targetOffset + 1] = (byte)(g / 4);
			target[targetOffset + 2] = (byte)(r / 4);
			target[targetOffset + 3] = (byte)(a / 4);
		}

		return target;
	}

	private readonly record struct PreviewVertex(float X, float Y, float Z);
	private static int RenderResolution => PreviewFrame.Resolution * RenderScale;
}
