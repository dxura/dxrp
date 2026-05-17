using System.Runtime.InteropServices;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public sealed partial class Glass
{
	private static readonly Vector2[] EdgeUVs = new[]
	{
		new Vector2( 0.0f, 0.0f ),
		new Vector2( 0.0f, 0.01f ),
		new Vector2( 0.01f, 0.01f ),
		new Vector2( 0.01f, 0.0f )
	};

	public Model CreateModel( List<Vector2> points )
	{
		var renderData = new RenderData();
		renderData.Init( points.Count );

		var average = Vector2.Zero;
		var pointCount = points.Count;

		if ( pointCount > 0 )
		{
			foreach ( var point in points )
			{
				average += point;
			}

			average /= pointCount;
		}

		var halfThickness = Thickness * 0.5f;

		renderData.Vertices?.Add( new Vector3( average.x, average.y, halfThickness ) );

		for ( var i = 0; i < renderData.FaceVertexCount - 1; i++ )
		{
			renderData.Vertices?.Add( new Vector3( points[i].x, points[i].y, halfThickness ) );
		}

		renderData.Vertices?.Add( new Vector3( average.x, average.y, -halfThickness ) );

		for ( var i = 0; i < renderData.FaceVertexCount - 1; i++ )
		{
			renderData.Vertices?.Add( new Vector3( points[i].x, points[i].y, -halfThickness ) );
		}

		var modelBuilder = new ModelBuilder();
		modelBuilder.AddCollisionHull( renderData.Vertices?.ToArray() );

		for ( var i = 0; i < renderData.EdgeQuadCount; i++ )
		{
			var next = i < renderData.EdgeQuadCount - 1 ? i + 1 : 0;
			renderData.Vertices?.Add( new Vector3( points[i].x, points[i].y, -halfThickness ) );
			renderData.Vertices?.Add( new Vector3( points[next].x, points[next].y, -halfThickness ) );
			renderData.Vertices?.Add( new Vector3( points[next].x, points[next].y, halfThickness ) );
			renderData.Vertices?.Add( new Vector3( points[i].x, points[i].y, halfThickness ) );
		}

		renderData.EdgeVerticesStart = renderData.TotalShardVertices - renderData.EdgeVertexCount;

		Assert.AreEqual( renderData.Vertices!.Count, renderData.TotalShardVertices );

		return modelBuilder.AddMesh( CreateMesh( renderData ) )
			.Create();
	}

	private Mesh CreateMesh( RenderData renderData )
	{
		var vertices = new Vertex[renderData.TotalShardVertices];
		var indices = new int[renderData.TotalSharedIndices];
		var bounds = new BBox();

		for ( var i = 0; i < renderData.TotalShardVertices; i++ )
		{
			vertices[i].Position = renderData.Vertices[i];
			bounds = bounds.AddPoint( vertices[i].Position );

			var vertexPos = new Vector3( renderData.Vertices[i].x, renderData.Vertices[i].y, 0 );
			var u = Vector3.Dot( TextureAxisU, vertexPos ) / TextureScale.x;
			var v = Vector3.Dot( TextureAxisV, vertexPos ) / TextureScale.y;

			u += TextureOffset.x;
			v += TextureOffset.y;

			u /= TextureSize.x;
			v /= TextureSize.y;

			var uv = new Vector2( u, v );

			vertices[i].TexCoord0 = uv;
			vertices[i].TexCoord1 = vertexPos;

			if ( i < renderData.EdgeVerticesStart )
			{
				vertices[i].Color = Vector3.Zero;
			}
			else
			{
				vertices[i].TexCoord0 += EdgeUVs[i % 4];
				vertices[i].TexCoord1 += EdgeUVs[i % 4];
				vertices[i].Color[0] = 1;
				vertices[i].Color[1] = 0;
				vertices[i].Color[2] = 0;
			}
		}

		ComputeTriangleNormalAndTangent( out var normalSideA, out var tangentSideA,
			vertices[1].Position, vertices[0].Position, vertices[2].Position,
			vertices[1].TexCoord1, vertices[0].TexCoord1, vertices[2].TexCoord1 );

		ComputeTriangleNormalAndTangent( out var normalSideB, out var tangentSideB,
			vertices[renderData.FaceVertexCount].Position, vertices[renderData.FaceVertexCount + 1].Position,
			vertices[renderData.FaceVertexCount + 2].Position,
			vertices[renderData.FaceVertexCount].TexCoord1, vertices[renderData.FaceVertexCount + 1].TexCoord1,
			vertices[renderData.FaceVertexCount + 2].TexCoord1 );

		for ( var i = 0; i < renderData.FaceTriangleCount; i++ )
		{
			var index = i * 3;
			var offset0 = i + 1;
			var offset1 = i + 2 < renderData.FaceVertexCount ? i + 2 : 1;
			var offset2 = 0;

			indices[index] = offset1;
			indices[index + 1] = offset0;
			indices[index + 2] = offset2;

			vertices[offset0].Normal = normalSideA;
			vertices[offset1].Normal = normalSideA;
			vertices[offset2].Normal = normalSideA;

			vertices[offset0].Tangent = tangentSideA;
			vertices[offset1].Tangent = tangentSideA;
			vertices[offset2].Tangent = tangentSideA;

			index += renderData.FaceIndexCount;
			offset0 += renderData.FaceVertexCount;
			offset1 += renderData.FaceVertexCount;
			offset2 = renderData.FaceVertexCount;

			indices[index] = offset0;
			indices[index + 1] = offset1;
			indices[index + 2] = offset2;

			vertices[offset0].Normal = normalSideB;
			vertices[offset1].Normal = normalSideB;
			vertices[offset2].Normal = normalSideB;

			vertices[offset0].Tangent = tangentSideB;
			vertices[offset1].Tangent = tangentSideB;
			vertices[offset2].Tangent = tangentSideB;
		}

		var edgeIndexOffset = renderData.TotalSharedIndices - renderData.EdgeIndexCount;
		for ( var i = 0; i < renderData.EdgeQuadCount; i++ )
		{
			var index = edgeIndexOffset + i * 6;
			var vertexOffset = renderData.EdgeVerticesStart + i * 4;

			indices[index] = vertexOffset + 2;
			indices[index + 1] = vertexOffset + 1;
			indices[index + 2] = vertexOffset;

			indices[index + 3] = vertexOffset + 3;
			indices[index + 4] = vertexOffset + 2;
			indices[index + 5] = vertexOffset;

			ComputeTriangleNormalAndTangent( out var faceNormal, out var faceTangent,
				vertices[vertexOffset + 2].Position, vertices[vertexOffset + 1].Position,
				vertices[vertexOffset].Position,
				vertices[vertexOffset + 2].TexCoord1, vertices[vertexOffset + 1].TexCoord1,
				vertices[vertexOffset].TexCoord1 );

			vertices[vertexOffset].Normal = faceNormal;
			vertices[vertexOffset + 1].Normal = faceNormal;
			vertices[vertexOffset + 2].Normal = faceNormal;
			vertices[vertexOffset + 3].Normal = faceNormal;

			vertices[vertexOffset].Tangent = faceTangent;
			vertices[vertexOffset + 1].Tangent = faceTangent;
			vertices[vertexOffset + 2].Tangent = faceTangent;
			vertices[vertexOffset + 3].Tangent = faceTangent;
		}

		var mesh = new Mesh( Material ?? Material.Load( "materials/glass.vmat" ) );
		mesh.CreateVertexBuffer<Vertex>( vertices.Length, Vertex.Layout, vertices );
		mesh.CreateIndexBuffer( indices.Length, indices );
		mesh.Bounds = bounds;

		return mesh;
	}

	[StructLayout( LayoutKind.Sequential )]
	private struct Vertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TexCoord0;
		public Vector2 TexCoord1;
		public Vector3 Color;
		public Vector4 Tangent;

		public static readonly VertexAttribute[] Layout =
		{
			new( VertexAttributeType.Position, VertexAttributeFormat.Float32 ),
			new( VertexAttributeType.Normal, VertexAttributeFormat.Float32 ),
			new( VertexAttributeType.TexCoord, VertexAttributeFormat.Float32, 2 ),
			new( VertexAttributeType.TexCoord, VertexAttributeFormat.Float32, 2, 1 ),
			new( VertexAttributeType.Color, VertexAttributeFormat.Float32 ),
			new( VertexAttributeType.Tangent, VertexAttributeFormat.Float32, 4 )
		};
	}

	private struct RenderData
	{
		public List<Vector3> Vertices;

		public int TotalShardVertices;
		public int TotalSharedIndices;
		public int EdgeVerticesStart;
		public int FaceVertexCount;
		public int FaceTriangleCount;
		public int FaceIndexCount;
		public int EdgeQuadCount;
		public int EdgeVertexCount;
		public int EdgeTriangleCount;
		public int EdgeIndexCount;

		public void Init( int numPanelVerts )
		{
			FaceVertexCount = numPanelVerts + 1;
			FaceTriangleCount = FaceVertexCount - 1;
			FaceIndexCount = FaceTriangleCount * 3;

			EdgeQuadCount = FaceVertexCount - 1;
			EdgeVertexCount = EdgeQuadCount * 4;
			EdgeTriangleCount = EdgeQuadCount * 2;
			EdgeIndexCount = EdgeTriangleCount * 3;

			TotalShardVertices = FaceVertexCount + FaceVertexCount + EdgeVertexCount;
			TotalSharedIndices = FaceIndexCount + FaceIndexCount + EdgeIndexCount;

			Vertices = new List<Vector3>( FaceVertexCount * 2 + EdgeVertexCount );
		}
	}
}
