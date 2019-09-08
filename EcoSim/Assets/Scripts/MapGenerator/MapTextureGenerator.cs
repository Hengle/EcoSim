﻿using System.Collections.Generic;
using UnityEngine;

public static class MapTextureGenerator
{
    private static Material drawingMaterial;


    public static Texture2D GenerateTexture(MapGraph map, int meshSize, int textureSize, List<MapGraph.NodeTypeColour> colours, bool drawBoundries, bool drawTriangles)
    {
        CreateDrawingMaterial();
        var texture = RenderGLToTexture(map, textureSize, meshSize, drawingMaterial, colours, drawBoundries, drawTriangles);

        return texture;
    }


    private static void CreateDrawingMaterial()
    {
        if (drawingMaterial)
        {
            return;
        }

        // Unity has a built-in shader that is useful for drawing
        // simple colored things.
        Shader shader   = Shader.Find("Hidden/Internal-Colored");
        drawingMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        // Turn on alpha blending
        drawingMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        drawingMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        // Turn backface culling off
        drawingMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        // Turn off depth writes
        drawingMaterial.SetInt("_ZWrite", 0);
    }

    private static Texture2D RenderGLToTexture(MapGraph map, int textureSize, int meshSize, Material material, List<MapGraph.NodeTypeColour> colours, bool drawBoundries, bool drawTriangles)
    {
        var renderTexture = CreateRenderTexture(textureSize, Color.white);

        DrawToRenderTexture(map, material, textureSize, meshSize, colours, drawBoundries, drawTriangles);

        return CreateTextureFromRenderTexture(textureSize, renderTexture);
    }

    private static Texture2D CreateTextureFromRenderTexture(int textureSize, RenderTexture renderTexture)
    {
        Texture2D newTexture = new Texture2D(textureSize, textureSize);
        newTexture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);

        bool applyMipsmaps = false;
        bool highQuality   = true;

        newTexture.Apply(applyMipsmaps);
        newTexture.Compress(highQuality);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        return newTexture;
    }

    private static RenderTexture CreateRenderTexture(int textureSize, Color color)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(textureSize, textureSize);
        RenderTexture.active        = renderTexture;

        GL.Clear(false, true, color);
        GL.sRGBWrite = false;
        
        return renderTexture;
    }

    private static void DrawToRenderTexture(MapGraph map, Material material, int textureSize, int meshSize, List<MapGraph.NodeTypeColour> colours, bool drawBoundries, bool drawTriangles)
    {
        material.SetPass(0);

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, meshSize, 0, meshSize);
        GL.Viewport(new Rect(0, 0, textureSize, textureSize));

        var coloursDictionary = new Dictionary<MapGraph.NodeType, Color>();
        foreach (var colour in colours)
        {
            if (!coloursDictionary.ContainsKey(colour.type))
            {
                coloursDictionary.Add(colour.type, colour.colour);
            }
        }

        DrawNodeTypes(map, coloursDictionary);

        if (drawBoundries)
        {
            DrawEdges(map, Color.black);
        }

        if (drawTriangles)
        {
            DrawDelaunayEdges(map, Color.red);
        }

        GL.PopMatrix();
    }

    private static void DrawEdges(MapGraph map, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);

        foreach (var edge in map.edges)
        {
            var start = edge.startPosition;
            var end   = edge.endPosition;

            GL.Vertex3(start.x, start.z, 0);
            GL.Vertex3(end.x,   end.z,   0);
        }

        GL.End();
    }

    private static void DrawDelaunayEdges(MapGraph map, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);

        foreach (var edge in map.edges)
        {
            if (edge.opposite != null)
            {
                var start = edge.node.centerPoint;
                var end   = edge.opposite.node.centerPoint;

                GL.Vertex3(start.x, start.z, 0);
                GL.Vertex3(end.x,   end.z,   0);
            }
        }

        GL.End();
    }

    private static void DrawNodeTypes(MapGraph map, Dictionary<MapGraph.NodeType, Color> colours)
    {
        GL.Begin(GL.TRIANGLES);

        foreach (var node in map.nodesByCenterPosition.Values)
        {
            var colour = colours.ContainsKey(node.nodeType) ? colours[node.nodeType] : Color.red;
            GL.Color(colour);

            foreach (var edge in node.GetEdges())
            {
                var start = edge.previous.destination.position;
                var end   = edge.destination.position;

                GL.Vertex3(node.centerPoint.x, node.centerPoint.z, 0);
                GL.Vertex3(start.x,            start.z,            0);
                GL.Vertex3(end.x,              end.z,              0);
            }
        }

        GL.End();
    }
}
