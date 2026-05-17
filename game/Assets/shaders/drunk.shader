
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 1
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		
		PixelInput i;
		i.vPositionPs = float4(v.vPositionOs.xy, 0.0f, 1.0f );
		i.vPositionWs = float3(v.vTexCoord, 0.0f);
		
		return i;
		
	}
}

PS
{
	#include "common/pixel.hlsl"
	#include "postprocess/functions.hlsl"
	#include "postprocess/common.hlsl"
	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );
		
	Texture2D g_tColorBuffer < Attribute( "ColorBuffer" ); SrgbRead ( true ); >;
	float g_flIntensity < Attribute( "intensity" ); Default1( 1.0 ); Range1( 0, 5 ); >;
	float g_flScalingFactor2 < UiGroup( ",0/,0/0" ); Default1( 0.1 ); Range1( 0, 1 ); >;
	float g_flDontTouch < UiGroup( ",0/,0/0" ); Default1( 0.23481488 ); Range1( 0, 1 ); >;
	float g_flScalingFactor < UiGroup( ",0/,0/0" ); Default1( 0.05 ); Range1( 0, 1 ); >;
	float g_flDrunkeneffectopacity < UiGroup( ",0/,0/0" ); Default1( 0.4270023 ); Range1( 0, 1 ); >;
		
	float2 MapSceneColorCoords( float2 vInput, float2 modes )
	{
		float2 result;
	
		// X
		if ( modes.x == 1 ) // Mirror
		{
			float xx = abs( vInput.x );
			result.x = (fmod( floor( xx ), 2.0 ) == 0.0) ? frac( xx ) : 1.0 - frac( xx );
		}
		else if ( modes.x == 2 ) // Clamp
		{
			result.x = clamp( vInput.x, 0.0, 1.0 );
		}
		else if ( modes.x == 3 ) // Border
		{
			result.x = (vInput.x < 0.0 || vInput.x > 1.0) ? 0.5 : vInput.x;
		}
		else if ( modes.x == 4 ) // MirrorOnce
		{
	        float xx = abs( vInput.x );
			float floorX = floor( xx );
			if ( floorX < 1.0 )
			{
				result.x = frac( xx );
			}
			else if ( floorX < 2.0 )
			{
				result.x = 1.0 - frac( xx );
			}
			else
			{
				result.x = vInput.x;
			}
		}
		else // Wrap by default
		{
			result.x = vInput.x;
		}
	
		// Y
		if ( modes.y == 1 ) // Mirror
		{
			float yy = abs( vInput.y );
			result.y = (fmod( floor( yy ), 2.0 ) == 0.0) ? frac( yy ) : 1.0 - frac( yy );
		}
		else if ( modes.y == 2 ) // Clamp
		{
			result.y = clamp( vInput.y, 0.0, 1.0 );
		}
		else if ( modes.y == 3 ) // Border
		{
			result.y = (vInput.y < 0.0 || vInput.y > 1.0) ? 0.5 : vInput.y;
		}
		else if ( modes.y == 4 ) // MirrorOnce
		{
			float yy = abs( vInput.y );
			float floorY = floor( yy );
			if ( floorY < 1.0 )
			{
				result.y = frac( yy );
			}
			else if ( floorY < 2.0 )
			{
				result.y = 1.0 - frac( yy );
			}
			else
			{
				result.y = vInput.y;
			}
		}
		else // Wrap by default
		{
			result.y = vInput.y;
		}
	
		return result;
	}
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float intensity = g_flIntensity;
		float2 l_0 = CalculateViewportUv( i.vPositionSs.xy );

		// Scale wobble factors by intensity
		float scaledFactor2 = g_flScalingFactor2 * intensity;
		float scaledDontTouch = g_flDontTouch * intensity;
		float scaledFactor = g_flScalingFactor * intensity;

		// Horizontal wobble
		float l_1 = l_0.x;
		float l_3 = g_flTime * 0.8;
		float l_4 = sin( l_3 );
		float l_5 = scaledFactor2 * l_4;
		float l_6 = l_5 + 0.3;
		float l_7 = l_1 - l_6;
		float l_8 = scaledFactor2 + 1;
		float l_9 = l_8 + l_5;
		float l_10 = 1 / l_9;
		float l_11 = l_7 * l_10;
		float l_12 = l_11 + l_6;

		// Vertical wobble
		float l_13 = l_0.y;
		float l_14 = sin( g_flTime );
		float l_16 = l_14 * scaledDontTouch;
		float l_17 = l_16 + 0.3;
		float l_18 = l_13 - l_17;
		float l_20 = scaledFactor + 1;
		float l_21 = scaledFactor * l_14;
		float l_22 = l_20 + l_21;
		float l_23 = 1 / l_22;
		float l_24 = l_18 * l_23;
		float l_25 = l_24 + l_17;

		float2 wobbledUv = clamp( float2( l_12, l_25 ), 0.001, 0.999 );

		// Chromatic aberration - scale split by intensity
		float chromaSplit = 0.01 * intensity;
		float3 l_29 = g_tColorBuffer.Sample( g_sAniso, wobbledUv ).rgb;
		float l_30 = l_29.x;
		float2 chromaUv = clamp( wobbledUv + float2( chromaSplit, chromaSplit ), 0.001, 0.999 );
		float3 l_32 = g_tColorBuffer.Sample( g_sAniso, chromaUv ).rgb;
		float l_33 = l_32.y;
		float l_34 = l_32.z;
		float4 l_35 = float4( l_30, l_33, l_34, 0 );

		// Blur - sample nearby pixels and average, scaled by intensity
		float blurRadius = 0.003 * intensity;
		float3 blurColor = float3( 0, 0, 0 );
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( blurRadius, 0 ), 0.001, 0.999 ) ).rgb;
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( -blurRadius, 0 ), 0.001, 0.999 ) ).rgb;
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( 0, blurRadius ), 0.001, 0.999 ) ).rgb;
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( 0, -blurRadius ), 0.001, 0.999 ) ).rgb;
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( blurRadius, blurRadius ) * 0.707, 0.001, 0.999 ) ).rgb;
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( -blurRadius, blurRadius ) * 0.707, 0.001, 0.999 ) ).rgb;
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( blurRadius, -blurRadius ) * 0.707, 0.001, 0.999 ) ).rgb;
		blurColor += g_tColorBuffer.Sample( g_sAniso, clamp( wobbledUv + float2( -blurRadius, -blurRadius ) * 0.707, 0.001, 0.999 ) ).rgb;
		blurColor /= 8.0;

		// Blend blur into the chromatic result based on intensity
		float blurStrength = saturate( intensity * 0.4 );
		float3 chromaticColor = l_35.rgb;
		float3 blended = lerp( chromaticColor, blurColor, blurStrength );

		return float4( blended, 1 );
	}
}
