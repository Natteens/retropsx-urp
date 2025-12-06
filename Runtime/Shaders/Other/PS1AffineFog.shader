Shader "Custom/PS1AffineFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LockSize ("Lock Size", float) = 0.1
        _XSnap("X Snap Scale", Range(1,1000)) = 50.0
	    _YSnap("Y Snap Scale", Range(1,1000)) = 50.0
	    _NearClipDarken ("Near Clip Darken", float) = 1
	    
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                noperspective float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LockSize;
            float _XSnap;
		    float _YSnap;
		    float _NearClipDarken;

            
            float4 calculateLighting()
            {
            
            }
            
            float4 calculateMainLight()
            {
                
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                //Cell locking                     
                float y = v.vertex.y;
                v.vertex = mul(unity_ObjectToWorld, v.vertex);
                v.vertex /= _LockSize;
                v.vertex = floor(v.vertex);
                v.vertex *= _LockSize;
                v.vertex = mul(unity_WorldToObject, v.vertex);
                v.vertex.y = y;
                
                
                //Vertice jitter
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float4 viewPos = mul(unity_MatrixV, worldPos);
                float4 clipPos = mul(unity_CameraProjection, viewPos);
                clipPos.xy /= clipPos.w;
                clipPos.xy = floor(clipPos.xy * float2(_XSnap, _YSnap) + 0.5) / float2(_XSnap, _YSnap);
                clipPos.xy *= clipPos.w;
                viewPos = mul(unity_CameraInvProjection, clipPos);            
                worldPos = mul(unity_MatrixInvV, viewPos);            
                v.vertex = mul(unity_WorldToObject, worldPos);

                //Near darken
                float4 vertexProgjPos = mul(UNITY_MATRIX_MV, v.vertex);
                if(_NearClipDarken > 0)
                    v.color.w *= saturate((-vertexProgjPos.z - _ProjectionParams.y) / (_NearClipDarken + 0.001));
                else
                    v.color.w /= saturate((-vertexProgjPos.z - _ProjectionParams.y) / (-_NearClipDarken + 0.001));
                    
                    
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 Color = tex2D(_MainTex, i.uv) * i.color.r;
                Color *= i.color.w;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, Color);
                
                return Color;
            }
            ENDCG
        }
    }
}
