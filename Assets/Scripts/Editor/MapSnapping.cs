using UnityEditor;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public static class MapSnapping
    {
        static MapSnapping()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            foreach (var gameObject in Selection.gameObjects)
            {
                if (gameObject.gameObject.CompareTag("Wall"))
                {
                    if (PrefabUtility.IsPartOfImmutablePrefab(gameObject)) continue;
                    
                    var position = gameObject.transform.position;

                    var rotation = Mathf.RoundToInt(gameObject.transform.eulerAngles.y / 90f);
                    var wallSize = new Vector2(5f, 4f) / 2f;
                    
                    position.x = Mathf.Round((position.x) / wallSize.x) * wallSize.x;
                    position.y = Mathf.Round(position.y / wallSize.y) * wallSize.y;
                    position.z = Mathf.Round((position.z) / wallSize.x) * wallSize.x;
                    
                    gameObject.transform.position = position;

                    gameObject.transform.eulerAngles = new Vector3(0f, rotation * 90f, 0f);
                }
                
                if (gameObject.gameObject.CompareTag("Floor"))
                {
                    if (PrefabUtility.IsPartOfImmutablePrefab(gameObject)) continue;
                    
                    var position = gameObject.transform.position;

                    var rotation = Mathf.RoundToInt(gameObject.transform.eulerAngles.y / 90f);
                    var wallSize = new Vector2(5f, 4f);
                    
                    position.x = Mathf.Round((position.x) / wallSize.x) * wallSize.x;
                    position.y = Mathf.Round(position.y / wallSize.y) * wallSize.y;
                    position.z = Mathf.Round((position.z) / wallSize.x) * wallSize.x;
                    
                    gameObject.transform.position = position;

                    gameObject.transform.eulerAngles = new Vector3(0f, rotation * 90f, 0f);
                }
            }
        }
    }
}