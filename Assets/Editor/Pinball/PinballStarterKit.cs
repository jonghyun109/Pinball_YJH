#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PinballStarterKit
{
    const string PixelPath = "Assets/Art/PinballPlaceholders/pixel.png";
    const string CirclePath = "Assets/Art/PinballPlaceholders/circle.png";
    const string WallMatPath = "Assets/Physics/PinballWall.physicsMaterial2D";
    const string BumperMatPath = "Assets/Physics/PinballBumper.physicsMaterial2D";
    const string BallMatPath = "Assets/Physics/PinballBall.physicsMaterial2D";

    [MenuItem("Pinball/플레이스홀더 테이블 생성")]
    static void CreatePlaceholderTable()
    {
        var existing = GameObject.Find("PinballWorld");
        if (existing != null && !EditorUtility.DisplayDialog("Pinball", "이미 PinballWorld가 있습니다. 지우고 다시 만들까요?", "다시 만들기", "취소"))
        {
            return;
        }

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        var pixel = EnsurePixelSprite();
        var circle = EnsureCircleSprite();
        var wallMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(WallMatPath);
        var bumperMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BumperMatPath);
        var ballMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BallMatPath);

        var world = new GameObject("PinballWorld");
        Undo.RegisterCreatedObjectUndo(world, "Create Pinball World");

        CreateBox(world.transform, "LeftWall", new Vector2(-3.73f, 0f), new Vector2(0.28f, 11.3f), new Color(0.22f, 0.32f, 0.5f), pixel, wallMat);
        CreateBox(world.transform, "RightWall", new Vector2(3.93f, 0f), new Vector2(0.28f, 11.3f), new Color(0.22f, 0.32f, 0.5f), pixel, wallMat);
        CreateBox(world.transform, "TopWall", new Vector2(0.1f, 5.51f), new Vector2(7.94f, 0.28f), new Color(0.22f, 0.32f, 0.5f), pixel, wallMat);
        CreateBox(world.transform, "BottomWall", new Vector2(0.1f, -5.63f), new Vector2(7.94f, 0.28f), new Color(0.22f, 0.32f, 0.5f), pixel, wallMat);
        CreateBox(world.transform, "LaneDivider", new Vector2(2.85f, -0.7f), new Vector2(0.28f, 9.8f), new Color(0.22f, 0.32f, 0.5f), pixel, wallMat);
        var ramp = CreateBox(world.transform, "ExitRamp", new Vector2(3.18f, 5.02f), new Vector2(1.7f, 0.28f), new Color(0.22f, 0.32f, 0.5f), pixel, wallMat);
        ramp.transform.rotation = Quaternion.Euler(0f, 0f, -38f);
        var shoulder = CreateBox(world.transform, "LeftShoulder", new Vector2(-3.05f, 4.95f), new Vector2(1.15f, 0.28f), new Color(0.22f, 0.32f, 0.5f), pixel, wallMat);
        shoulder.transform.rotation = Quaternion.Euler(0f, 0f, 32f);

        var pegs = new GameObject("Pegs");
        pegs.transform.SetParent(world.transform, false);
        int row = 0;
        for (float y = 3.95f; y >= -2.15f; y -= 0.78f)
        {
            float offset = (row % 2 == 0) ? 0f : 0.39f;
            int index = 0;
            for (float x = -3.05f + offset; x <= 2.45f; x += 0.78f)
            {
                CreatePeg(pegs.transform, $"Peg_{row}_{index}", new Vector2(x, y), 0.31f, circle, bumperMat);
                index++;
            }

            row++;
        }

        int[] scores = { 100, 200, 500, 200, 100 };
        var colors = new[]
        {
            new Color(0.28f, 0.46f, 0.78f),
            new Color(0.27f, 0.62f, 0.58f),
            new Color(0.82f, 0.42f, 0.22f),
            new Color(0.27f, 0.62f, 0.58f),
            new Color(0.28f, 0.46f, 0.78f)
        };
        float playLeft = -3.59f;
        float playRight = 2.71f;
        float width = (playRight - playLeft) / scores.Length;
        var pockets = new GameObject("Pockets");
        pockets.transform.SetParent(world.transform, false);
        for (int i = 0; i < scores.Length; i++)
        {
            float x = playLeft + width * (i + 0.5f);
            CreatePocket(pockets.transform, $"Pocket_{i}", new Vector2(x, -5.05f), new Vector2(width - 0.08f, 0.95f), colors[i], scores[i], pixel);
            if (i > 0)
            {
                CreateBox(pockets.transform, $"PocketDivider_{i}", new Vector2(playLeft + width * i, -4.55f), new Vector2(0.12f, 1.85f), new Color(0.75f, 0.8f, 0.9f), pixel, wallMat);
            }
        }

        var ball = CreateBall(world.transform, new Vector2(3.39f, -5.27f), 0.4f, circle, ballMat);
        var rest = new GameObject("RestPoint");
        rest.transform.SetParent(world.transform, false);
        rest.transform.position = new Vector2(3.39f, -5.27f);
        var launcher = CreateLauncher(world.transform, rest.transform, new Vector2(3.39f, -5.44f), pixel);

        var returnZone = CreateTrigger(world.transform, "LaunchReturnZone", new Vector2(3.39f, -2.4f), new Vector2(0.7f, 5.6f));
        returnZone.AddComponent<BallReturnZone>();

        var game = Object.FindFirstObjectByType<PinballGame>();
        if (game != null)
        {
            var so = new SerializedObject(game);
            so.FindProperty("launcher").objectReferenceValue = launcher;
            so.ApplyModifiedProperties();
        }

        Selection.activeGameObject = world;
        Debug.Log("플레이스홀더 테이블을 만들었습니다. 각 오브젝트의 Sprite Renderer에 이미지를 넣으면 됩니다.");
    }

    static GameObject CreateBox(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite, PhysicsMaterial2D material)
    {
        var go = CreateSpriteObject(parent, name, position, size, color, sprite, 0);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.sharedMaterial = material;
        return go;
    }

    static void CreatePeg(Transform parent, string name, Vector2 position, float diameter, Sprite sprite, PhysicsMaterial2D material)
    {
        var go = CreateSpriteObject(parent, name, position, new Vector2(diameter, diameter), new Color(0.95f, 0.74f, 0.28f), sprite, 2);
        var collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.sharedMaterial = material;
        go.AddComponent<Bumper>();
    }

    static void CreatePocket(Transform parent, string name, Vector2 position, Vector2 size, Color color, int points, Sprite sprite)
    {
        var go = CreateSpriteObject(parent, name, position, size, color, sprite, -5);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        var pocket = go.AddComponent<ScorePocket>();
        var so = new SerializedObject(pocket);
        so.FindProperty("points").intValue = points;
        so.ApplyModifiedProperties();
    }

    static PinballBall CreateBall(Transform parent, Vector2 position, float diameter, Sprite sprite, PhysicsMaterial2D material)
    {
        var go = CreateSpriteObject(parent, "Ball", position, new Vector2(diameter, diameter), new Color(0.93f, 0.95f, 0.98f), sprite, 10);
        var collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.sharedMaterial = material;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = false;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.gravityScale = 1.15f;
        body.linearDamping = 0.05f;
        body.angularDamping = 0.08f;
        return go.AddComponent<PinballBall>();
    }

    static BallLauncher CreateLauncher(Transform parent, Transform restPoint, Vector2 plungerPosition, Sprite sprite)
    {
        var plunger = CreateSpriteObject(parent, "Plunger", plungerPosition, new Vector2(0.58f, 0.16f), new Color(0.86f, 0.35f, 0.32f), sprite, 8);
        var host = new GameObject("Launcher");
        host.transform.SetParent(parent, false);
        host.transform.position = plungerPosition;
        var launcher = host.AddComponent<BallLauncher>();
        var so = new SerializedObject(launcher);
        so.FindProperty("restPoint").objectReferenceValue = restPoint;
        so.ApplyModifiedProperties();
        return launcher;
    }

    static GameObject CreateTrigger(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        return go;
    }

    static GameObject CreateSpriteObject(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    static Sprite EnsurePixelSprite()
    {
        return EnsureSprite(PixelPath, 4, false);
    }

    static Sprite EnsureCircleSprite()
    {
        return EnsureSprite(CirclePath, 64, true);
    }

    static Sprite EnsureSprite(string path, int size, bool circle)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        EnsureFolder("Assets/Art", "Art");
        EnsureFolder("Assets/Art/PinballPlaceholders", "PinballPlaceholders");

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!circle)
                {
                    tex.SetPixel(x, y, Color.white);
                    continue;
                }

                float cx = (size - 1) * 0.5f;
                float radius = size * 0.5f - 1.25f;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
                float alpha = Mathf.Clamp01(radius - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = circle ? FilterMode.Bilinear : FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureFolder(string path, string folderName)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
