Shader "Unlit/MultiplyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // 1. CHANGED: Switched from Opaque to Transparent queue
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // 2. ADDED: This is the magic blend mode that forces MULTIPLY
        Blend Zero SrcColor 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // REMOVED: Fog multi-compile line is gone

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                // REMOVED: Fog coordinates line is gone
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // REMOVED: Fog transfer line is gone
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample your C# generated color buffer texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // REMOVED: Fog application line is gone
                
                return col;
            }
            ENDCG
        }
    }
}
