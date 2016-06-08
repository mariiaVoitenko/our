/*
*
*
* Road
*
*/

using System;
using UnityEngine;

namespace SpaceBeat.Objects3D
{
  public class Track
  {
    private double[] _fluxThresholds;

    private int _segmentsH;
    private float _width;
    private float _depth;
    private float _height;

    private GameObject _instance = new GameObject();

    public Track(float width, float height, float depth, Color color, double[] fluxThresholds)
    {
      _instance.name = "Track";
      _fluxThresholds = fluxThresholds;
      _depth = depth;
      _width = width;
      _height = (fluxThresholds.Length - 1) * height;
      _segmentsH = fluxThresholds.Length - 1;

      var meshRenderer = _instance.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
      meshRenderer.material = (Material)Resources.Load("TrackMaterial");

      Build();
    }

    private void Build()
    {
      Vector3[] vertices = new Vector3[2 * (_segmentsH + 1)];
      Vector3[] normals = new Vector3[2 * (_segmentsH + 1)];
      Vector2[] uvs = new Vector2[2 * (_segmentsH + 1)];
      int[] triangles = new int[6 * _segmentsH];

      float x, y, z;
      float ny, nz;
      float sny, snz, s;

      int numIndices = 0;
      int index = 0;
      int basis;

      sny = snz = 0.0f;
      for (int yi = 0; yi <= _segmentsH; yi++)
      {
        for (int xi = 0; xi <= 1; xi++)
        {
          x = (xi - 1) * _width;
          y = (float)_fluxThresholds[_segmentsH - yi] * _depth;
          z = -((float)yi / (float)_segmentsH - 1) * _height;
          vertices[index] = new Vector3(x, y, z);

          if (xi == 0)
          {
            ny = -_width * ((((yi + 1) / _segmentsH - 1) * _height) - y);
            nz = (float)((yi != 0) ? -_width * (_fluxThresholds[_segmentsH - yi + 1] * _depth - _fluxThresholds[_segmentsH - yi] * _depth) : 1);

            s = (float)Math.Sqrt(ny * ny + nz * nz);
            sny = Math.Abs(ny / s);
            snz = nz / s;
          }

          normals[index] = new Vector3(0, sny, snz);

          if (xi == 0 && yi != _segmentsH)
          {
            basis = 2 * yi;
            triangles[numIndices++] = (basis + 1);
            triangles[numIndices++] = (basis + 2 + 1);
            triangles[numIndices++] = basis;
            triangles[numIndices++] = (basis + 2 + 1);
            triangles[numIndices++] = (basis + 2);
            triangles[numIndices++] = basis;
          }

          uvs[index] = new Vector2(
               xi * (_width / 512),
               ((float)yi / (float)_segmentsH) * _height / 512
               );
          index++;
        }
      }

      Mesh mesh = new Mesh
      {
        vertices = vertices,
        triangles = triangles,
        normals = normals,
        uv = uvs
      };

      mesh.RecalculateNormals();

      var meshFilter = (MeshFilter)_instance.AddComponent(typeof(MeshFilter));
      meshFilter.mesh = mesh;
    }
  }
}
