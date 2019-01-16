Shader "Mobile/TerrainColorShader"
{
    Properties
    {
        _Color("Color",COLOR)=(0.5,0.5,0.5,1.0)
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma target 3.5
		#pragma surface surf Lambert noforwardadd vertex:vert nolightmap

        sampler2D _MainTex;
        fixed4 _Color;
 
		struct appdata {
        	nointerpolation float4 color : COLOR;
            float4 texcoord : TEXCOORD0;
        
			float4 vertex : POSITION;
			float3 normal : NORMAL;
		};

        struct Input
        {
            float2 uv_MainTex;
			nointerpolation float4 color;
        };

		void vert(inout appdata v, out Input o)
		{
			o.color = v.color;
			o.uv_MainTex = v.texcoord;
		}
 
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    Fallback "Mobile/VertexLit"
}