/// <summary>
/// This is a more primitive version of Roy Stan's geometry shader, based also on an old shader made by by Jiadong Chen http://www.jiadongchen.com/
/// Currently a work in progress and the wind maths is not yet working properly.
/// This current version has twice the vertex data that Roy's does and as such requires twice as much work from the GPU
/// because I am drawing rectangles rather than triangles in order to have more intuitive control over the mesh UVs for now.
/// But this allows me to use the AlphaTest value (values of 0 in the texture are not rendered) from a texture as the visual shape of the grass blades instead of being stuck rendering triangles.
/// This decision also avoids the pixelation artifacts around the individual grass blades that Roy mentions near the end of his tutorial.
/// Change the scene view shading to 'Wireframe' in the top left (by default it's set to Shaded in the Scene view) after you draw a few blades of grass to see what I mean.
/// Roy Stan's original tutorial is available <a href="https://roystan.net/articles/grass-shader.html">here</a> where he provides a more thorough explanation of how a geometry shader works.
/// <summary>
Shader "GeometryShader/Grass" {
	/// <summary>
	/// Declare the properties to draw in the inspector
	/// Unity links these properties to the variables below in the CGPROGRAM only if they have the EXACT same name (Case Sensitive) as those variables
	/// </summary>
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {} //the 2D property ensures the material inspector creates an interactable Texture space in the Inspector GUI
		_AlphaTex("Alpha (A)", 2D) = "white" {}
		_Height("Grass Height", range(0.1,5)) = 3 //the range property limits the range that the user can set these properties to in the inspector between two numbers (min,max)
		_Width("Grass Width", range(0, 0.1)) = 0.05
		_WindOscillationStrength("Wind Strength", range(0,5)) = 2.5

	}
	SubShader{
		/// <summary>
		/// Cull has 3 settings:
		/// 'back' ensures that the back faces (as determined by the mesh normals) are transparent/invisible
		/// 'front' ensures that the front faces (as determined by the mesh normals) are transparent/invisible
		/// 'off' renders data to both sides of the mesh if the shader has data for both sides
		/// </summary>
		Cull off
			/// <summary>
			/// tags determine how Unity will render this shader and the 'Queue' tag determines where in the rendering order this shader will occur
			/// For special uses in-between queues can be used. (Say for example you want to render a road and then specifically render puddles on top of the road)
			/// Internally each queue is represented by integer index. By default the integer values for each rendering layer are:
			/// Background is 1000, Geometry is 2000, Transparent is 3000 and Overlay is 4000.
			/// <a href="https://docs.unity3d.com/Manual/SL-SubShaderTags.html">Shader Tags Documentation</a>
			/// </summary>
			Tags{ "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }

		Pass
		{

			Cull OFF
			Tags{ "LightMode" = "ForwardBase" }
			AlphaToMask On


			CGPROGRAM

			#include "UnityCG.cginc" 
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#include "UnityLightingCommon.cginc" // Used to get scene lighting data, still trying to figure this out tbh.
			// If the grass you're drawing is showing up black even after you've set the texture:
			// for now change the direction of the main scene lighting so it's perpendicular to the grass blades

			/// <summary>
			/// Setting the target rendering to a certain value ensures that the shader will only run if the GPU of the current device is capable of rendering it.
			/// Geometry shaders only work on platforms that run DX11+ SM5.0 or OpenGL3.2+. Currently Vulkan does not support geometry shaders.
			/// This means that geometry shaders are not supported on any mobile platform including the Oculus Rift and the Nintendo Switch
			/// PS4,PS5, XBox1, XboxX, and any PC running Mac or Windows made in the last 10 years all support geometry shaders
			/// Unity Documentation on this is available <a href="https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html">here</a>.
			/// </summary>
			#pragma target 4.0

			/// <summary>
			/// _MainTex data is pulled from the _MainTex property declared at the top of this shader and set in the inspector
			/// only because it has the EXACT same name (Case Sensitive) as that property.
			/// Shader convention in Unity is to use CamelCase preceded by an _Underscore for all properties set in the inspector.
			/// </summary>
			sampler2D _MainTex;
			sampler2D _AlphaTex;

			float _Height;
			float _Width;
			float _WindOscillationStrength;

			/// <summary>
			/// These structs allow us to get scene and mesh data using built in Unity properties.
			/// Unity's shader properties can be a bit obtuse, and sometimes the names aren't what you'd expect.
			/// Make liberal use of tutorials and <a href="https://docs.unity3d.com/Manual/SL-ShaderSemantics.html">Unity's Documentation</a> while you're learning.
			/// </summary>
			struct v2g
			{
				float4 pos : SV_POSITION;
				float3 norm : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct g2f
			{
				float4 pos : SV_POSITION;
				float3 norm : NORMAL;
				float2 uv : TEXCOORD0;
			};


			static const float oscillateDelta = 0.05;


		//Getting vertex data
		v2g vert(appdata_full v)
		{
			v2g o;
			o.pos = v.vertex;
			o.norm = v.normal;
			o.uv = v.texcoord;

			return o;
		}

		//Creating new blank vertex data which we will add to later
		g2f createGSOut() {
			g2f output;

			output.pos = float4(0, 0, 0, 0);
			output.norm = float3(0, 0, 0);
			output.uv= float2(0, 0);

			return output;
		}


		//Constructing new mesh geometry based on current vertex data
		[maxvertexcount(30)]
		void geom(point v2g points[1], inout TriangleStream<g2f> triStream)
		{
		 
			float4 root = points[0].pos;

			const int vertexCount = 12;

			float random = sin(UNITY_HALF_PI * frac(root.x) + UNITY_HALF_PI * frac(root.z));


			_Width = _Width + (random / 50);
			_Height = _Height + (random / 5);


			//This is where I'm creating new mesh quads
			g2f v[vertexCount] = {
				createGSOut(), createGSOut(), createGSOut(), createGSOut(),
				createGSOut(), createGSOut(), createGSOut(), createGSOut(),
				createGSOut(), createGSOut(), createGSOut(), createGSOut()
			};

			//Texture coordinates
			float currentV = 0;
			float offsetV = 1.f /((vertexCount / 2) - 1);

			float currentVertexHeight = 0;

			//Wind influence, currently not working right
			float windCoEff = 0;

			//This for loop is where all the fun mesh building maths is.
			//This is very similar to generating procedural mesh at runtime on the CPU in C# and the maths is identical.
			for (int i = 0; i < vertexCount; i++)
			{
				v[i].norm = float3(0, 0, 1);

				if (fmod(i , 2) == 0)
				{ 
					v[i].pos = float4(root.x - _Width , root.y + currentVertexHeight, root.z, 1);
					v[i].uv = float2(0, currentV);
				}
				else
				{ 
					v[i].pos = float4(root.x + _Width , root.y + currentVertexHeight, root.z, 1);
					v[i].uv = float2(1, currentV);

					currentV += offsetV;
					currentVertexHeight = currentV * _Height;
				}

				float2 wind = float2(sin(_Time.x * UNITY_PI * 5), sin(_Time.x * UNITY_PI * 5));
				wind.x += (sin(_Time.x + root.x / 25) + sin((_Time.x + root.x / 15) + 50)) * 0.5;
				wind.y += cos(_Time.x + root.z / 80);
				wind *= lerp(0.7, 1.0, 1.0 - random);

				float sinSkewCoeff = random;
				float lerpCoeff = (sin(_WindOscillationStrength * _Time.x + sinSkewCoeff) + 1.0) / 2;
				float2 leftWindBound = wind * (1.0 - oscillateDelta);
				float2 rightWindBound = wind * (1.0 + oscillateDelta);

				wind = lerp(leftWindBound, rightWindBound, lerpCoeff);

				float randomAngle = lerp(-UNITY_PI, UNITY_PI, random);
				float randomMagnitude = lerp(0, 1., random);
				float2 randomWindDir = float2(sin(randomAngle), cos(randomAngle));
				wind += randomWindDir * randomMagnitude;

				float windForce = length(wind);

				v[i].pos.xz += wind.xy * windCoEff;
				v[i].pos.y -= windForce * windCoEff * 0.8;

				v[i].pos = UnityObjectToClipPos(v[i].pos);

				if (fmod(i, 2) == 1) {

					windCoEff += offsetV;
				}

			}

			//Setting the new mesh triangles
			for (int p = 0; p < (vertexCount - 2); p++) {
				triStream.Append(v[p]);
				triStream.Append(v[p + 2]);
				triStream.Append(v[p + 1]);
			}
		}


		/// <summary>
		/// This function is where all the lighting and color data happens.
		/// Because the grass mesh data is flat the lighting is not working how you might expect and I'll probably need to modify the grass normals to achieve a more even look.
		/// A great explanation on what I mean by "modify the normals" is available <a href="https://simonschreibt.de/gat/airborn-trees/">here</a>.
		/// </summary>
		half4 frag(g2f IN) : COLOR
		{
			fixed4 color = tex2D(_MainTex, IN.uv);
			fixed4 alpha = tex2D(_AlphaTex, IN.uv);

			half3 worldNormal = UnityObjectToWorldNormal(IN.norm);

			//scene lighting data will be multiplied by the color of the grass
			//currently the shader doesn't make use of point lights
			fixed3 light;

			//ambient
			fixed3 ambient = ShadeSH9(half4(worldNormal, 1));

			//diffuse
			fixed3 diffuseLight = saturate(dot(worldNormal, UnityWorldSpaceLightDir(IN.pos))) * _LightColor0;

			//specular Blinn-Phong
			//An explanation of Blinn-Phong lighting is available <a href="https://medium.com/shader-coding-in-unity-from-a-to-z/light-in-computer-graphics-2-3b2a5b04ac6d">here</a>.
			fixed3 halfVector = normalize(UnityWorldSpaceLightDir(IN.pos) + WorldSpaceViewDir(IN.pos));
			fixed3 specularLight = pow(saturate(dot(worldNormal, halfVector)), 15) * _LightColor0;

			light = ambient + diffuseLight + specularLight;

			return float4(color.rgb * light, alpha.g);

		}
		ENDCG

	}
	}
}