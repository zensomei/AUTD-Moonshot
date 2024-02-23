using System;
using UnityEngine;
using Intel.RealSense;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointsCalculator : MonoBehaviour
{
    public RsFrameProvider Source;
    private Mesh mesh;
    private Texture2D uvmap;

    [NonSerialized]
    private Vector3[] vertices;

    FrameQueue q;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += Dispose;
    }

    private void OnStartStreaming(PipelineProfile obj)
    {
        q = new FrameQueue(1);

        using (var depth = obj.Streams.FirstOrDefault(s => s.Stream == Stream.Depth && s.Format == Format.Z16).As<VideoStreamProfile>())
            ResetMesh(depth.Width, depth.Height);

        Source.OnNewSample += OnNewSample;
    }

    private void ResetMesh(int width, int height)
    {
        Assert.IsTrue(SystemInfo.SupportsTextureFormat(TextureFormat.RGFloat));
        uvmap = new Texture2D(width, height, TextureFormat.RGFloat, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
        };
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_UVMap", uvmap);

        if (mesh != null)
            mesh.Clear();
        else
            mesh = new Mesh()
            {
                indexFormat = IndexFormat.UInt32,
            };

        vertices = new Vector3[width * height];

        var indices = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            indices[i] = i;

        mesh.MarkDynamic();
        mesh.vertices = vertices;

        var uvs = new Vector2[width * height];
        Array.Clear(uvs, 0, uvs.Length);
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                uvs[i + j * width].x = i / (float)width;
                uvs[i + j * width].y = j / (float)height;
            }
        }

        mesh.uv = uvs;

        mesh.SetIndices(indices, MeshTopology.Points, 0, false);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    //void OnDestroy()
    //{
    //    if (q != null)
    //    {
    //        q.Dispose();
    //        q = null;
    //    }

    //    if (mesh != null)
    //        Destroy(null);
    //}

    void OnDestroy()
    {
        if (q != null)
        {
            q.Dispose();
            q = null;
        }

        if (mesh != null)
        {
            Destroy(mesh);
        }
    }

    private void Dispose()
    {
        Source.OnNewSample -= OnNewSample;

        if (q != null)
        {
            q.Dispose();
            q = null;
        }
    }

    private void OnNewSample(Frame frame)
    {
        if (q == null)
            return;
        try
        {
            if (frame.IsComposite)
            {
                using (var fs = frame.As<FrameSet>())
                using (var points = fs.FirstOrDefault<Points>(Stream.Depth, Format.Xyz32f))
                {
                    if (points != null)
                    {
                        q.Enqueue(points);
                    }
                }
                return;
            }

            if (frame.Is(Extension.Points))
            {
                q.Enqueue(frame);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private Vector3 center;
    public Vector3 Center
    {
        get { return this.center; }
        private set { this.center = value; }
    }

    protected void Update()
    {
        if (q != null)
        {
            Points points;
            if (q.PollForFrame<Points>(out points))
                using (points)
                {
                    if (points.Count != mesh.vertexCount)
                    {
                        using (var p = points.GetProfile<VideoStreamProfile>())
                            ResetMesh(p.Width, p.Height);
                    }

                    if (points.TextureData != IntPtr.Zero)
                    {
                        uvmap.LoadRawTextureData(points.TextureData, points.Count * sizeof(float) * 2);
                        uvmap.Apply();
                    }

                    if (points.VertexData != IntPtr.Zero)
                    {
                        points.CopyVertices(vertices);

                        mesh.vertices = vertices;
                        mesh.UploadMeshData(false);

                        // 臒l���̓_�Q�̒��S���W���v�Z
                        Vector3 sum = Vector3.zero;
                        int count = 0;
                        foreach (var vertex in vertices)
                        {
                            // �����ɍ��v���邩�`�F�b�N
                            if (vertex.z > 0.1f && vertex.z <= 0.35f && vertex.x > -0.4f && vertex.x <= 0.2f)
                            {
                                // Y���W�̏㉺���t�ɂ���
                                Vector3 invertedYVertex = new Vector3(vertex.x, -vertex.y, vertex.z);

                                sum += invertedYVertex;
                                count++;
                            }
                        }

                        if (count > 10000)
                        {
                            // �����ɍ��v����_�Q�̐����o��
                            //Debug.Log($"Filtered points count: {count}");

                            // ���S���W���v�Z
                            center = sum / count;

                            // ���S���W���o��
                            Debug.Log($"{count}points, Center of points (Y inverted): {center}");
                        }
                        else
                        {
                            Debug.Log("Points < 10000.");
                        }
                    }

                }
        }
    }
}