using UnityEditor;
using UnityEngine;

public static class LensUvRebaker
{
    private const string Lens1Path = "Assets/LensGames/Meshes/Lens1DisplayMesh.asset";
    private const string Lens2Path = "Assets/LensGames/Meshes/Lens2DisplayMesh.asset";

    [MenuItem("Tools/Lenses/Rebake Lens Display UVs (planar)")]
    public static void RebakeAll()
    {
        Rebake(Lens1Path);
        Rebake(Lens2Path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LensUvRebaker] Done.");
    }

    private static void Rebake(string path)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            Debug.LogError("[LensUvRebaker] Mesh not found: " + path);
            return;
        }

        Vector3[] vertices = mesh.vertices;
        if (vertices == null || vertices.Length == 0)
        {
            Debug.LogError("[LensUvRebaker] No vertices: " + path);
            return;
        }

        Vector3 min = vertices[0];
        Vector3 max = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        Vector3 size = max - min;

        // Smallest extent axis = the lens normal. The other two axes form the lens plane.
        int normalAxis = 0;
        if (size[1] < size[normalAxis]) normalAxis = 1;
        if (size[2] < size[normalAxis]) normalAxis = 2;

        int uAxis = (normalAxis + 1) % 3;
        int vAxis = (normalAxis + 2) % 3;

        // Make the wider planar axis = U so the source RT (square) maps wide->wide.
        if (size[vAxis] > size[uAxis])
        {
            int tmp = uAxis;
            uAxis = vAxis;
            vAxis = tmp;
        }

        float uMin = min[uAxis];
        float uRange = Mathf.Max(1e-6f, size[uAxis]);
        float vMin = min[vAxis];
        float vRange = Mathf.Max(1e-6f, size[vAxis]);

        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            float u = (vertices[i][uAxis] - uMin) / uRange;
            float v = (vertices[i][vAxis] - vMin) / vRange;
            uv[i] = new Vector2(u, v);
        }

        Undo.RecordObject(mesh, "Rebake Lens Display UVs");
        mesh.uv = uv;
        EditorUtility.SetDirty(mesh);

        Debug.Log($"[LensUvRebaker] {path} :: normalAxis={normalAxis} uAxis={uAxis} vAxis={vAxis} verts={vertices.Length}");
    }
}
