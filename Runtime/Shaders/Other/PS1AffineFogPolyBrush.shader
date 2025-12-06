// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/PS1AffineFogPolyBrush"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LockSize ("Lock Size", float) = 0.1
        _XSnap("X Snap Scale", Range(1,1000)) = 50.0
	    _YSnap("Y Snap Scale", Range(1,1000)) = 50.0
	    _NearClipDarken ("Near Clip Darken", float) = 1
	    
	    //For polyBrush
	    _Texture1 ("Texture", 2D) = "white" {}
	    _Texture2 ("Texture", 2D) = "white" {}
	    _Texture3 ("Texture", 2D) = "white" {}
	    
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
            
            #pragma Lambert vertex:vert
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            
            //PolyBrush stuff
            #define Z_TEXTURE_CHANNELS 4
            #define Z_MESH_ATTRIBUTES COLOR

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
                float4 nearDepth: TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LockSize;
            float _XSnap;
		    float _YSnap;
		    float _NearClipDarken;
        
            sampler2D _Texture1;
            sampler2D _Texture2;
            sampler2D _Texture3;
        

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
                //o.depthColor = v.color;
                //o.depthColor = fixed4(1,1,1,1);
                float4 vertexProgjPos = mul(UNITY_MATRIX_MV, v.vertex);
                o.nearDepth = v.color;
                o.nearDepth *= saturate((-vertexProgjPos.z - _ProjectionParams.y) / (_NearClipDarken + 0.001));
                 
                //Unfortunatly near depth can't be done in fragment shader when working with polybrush as far as I can tell
                o.color = v.color;
                o.color *= o.nearDepth;
                 
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 colMain = tex2D(_MainTex, i.uv) * i.color.r;
                fixed4 tcol1 = tex2D(_Texture1, i.uv) * i.color.g;
                fixed4 tcol2 = tex2D(_Texture2, i.uv) * i.color.b;
                fixed4 tcol3 = tex2D(_Texture3, i.uv) * i.color.a;
            
                fixed4 finalColor = colMain + tcol1 + tcol2 + tcol3;
                
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
}
