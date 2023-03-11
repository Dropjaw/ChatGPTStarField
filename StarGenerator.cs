using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using System;
using UnityEngine.UI;

public class StarGenerator : MonoBehaviour
{
    public GameObject entityPrefab;
    public string dataSourceUrl = "https://raw.githubusercontent.com/astronexus/HYG-Database/master/hygdata_v3.csv";

    private List<EntityData> closestEntities;

    // Start is called before the first frame update
    void Start()
    {
        // Load the dataset from the CSV file
        var data = LoadDataFromCSV(dataSourceUrl);

        // Sort the data by distance from the Sun
        var sortedData = data.OrderBy(d => d.Distance);

        // Take the closest 5000 entities
        closestEntities = sortedData.Take(5000).ToList();

        // Calculate the required scale
        float maxDistance = sortedData.Take(10000).ToList()[9999].Distance;
        float minDistance = sortedData.Take(1).ToList()[0].Distance;
        float requiredScaleFactor = maxDistance / Camera.main.farClipPlane;

        // Scale down the positions of all entities
        foreach (var star in data)
        {
            star.X /= requiredScaleFactor;
            star.Y /= requiredScaleFactor;
            star.Z /= requiredScaleFactor;
        }

        // Create game objects for each entity
        foreach (var entity in closestEntities)
        {
            GameObject newEntity = Instantiate(entityPrefab);

            // Create the position of the entity
            Vector3 starPosition = new(entity.X , entity.Y , entity.Z);

            // Set the position and color of the entity game object
            newEntity.transform.position = starPosition;
            newEntity.GetComponent<Renderer>().material.color = entity.Color;
            newEntity.name = entity.Name;

            StartCoroutine(MoveToNextEntity());
        }
    }

    // Version 2
    private IEnumerator MoveCameraTowardsEntity(EntityData entity, float duration)
    {
        // Get the current position and rotation of the camera
        Vector3 startPosition = Camera.main.transform.position;
        Quaternion startRotation = Camera.main.transform.rotation;

        // Calculate the target position just before the entity
        Vector3 entityPosition = new Vector3(entity.X, entity.Y, entity.Z);
        float distanceToEntity = Vector3.Distance(startPosition, entityPosition);
        Vector3 targetPosition = startPosition + (entityPosition - startPosition).normalized * (distanceToEntity - 2f);

        // Interpolate between the current position and rotation and the target position and rotation over the specified duration
        Quaternion targetRotation = Quaternion.LookRotation(entityPosition - targetPosition);
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            Camera.main.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            Camera.main.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Set the final position and rotation of the camera to the target position and rotation
        Camera.main.transform.position = targetPosition;
        Camera.main.transform.rotation = targetRotation;
        yield return null;
    }

    private IEnumerator MoveToNextEntity()
    {
        for (int i = 0; i < closestEntities.Count; i++)
        {
            EntityData entity = closestEntities[i];

            // Move the camera towards the current entity
            yield return StartCoroutine(MoveCameraTowardsEntity(entity, 3f));

            // Wait for 1 second before moving to the next entity
            yield return new WaitForSeconds(1f);
        }
    }

    private List<EntityData> LoadDataFromCSV(string url)
    {
        List<EntityData> data = new List<EntityData>();

        using (var webClient = new System.Net.WebClient())
        {
            var csvData = webClient.DownloadString(url);
            var lines = csvData.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var fields = lines[i].Split(',');

                    // Parse the fields from the CSV row
                    var entity = new EntityData();
                    entity.Name = fields[6];
                    entity.X = float.Parse(fields[17]);
                    entity.Y = float.Parse(fields[18]);
                    entity.Z = float.Parse(fields[19]);
                    entity.Distance = float.Parse(fields[9]);
                    entity.Color = GetStarColor(float.Parse(fields[16]));

                    // Add the entity to the data list
                    data.Add(entity);
                }
                catch (Exception e)
                {
                    string ee = e.Message;
                }
            }
        }

        data = data.OrderBy(e => e.Distance).ToList();

        for (int x = 0; x < data.Count; x++)
        {
            try
            {
                for (int y = x + 1; y < x + 2; y++)
                {
                    if (distanceTo(data[x], data[y]) < 5)
                    {
                        data.RemoveAt(y);
                        y--;
                    }
                }
            }
            catch { }
        }

        return data;
    }

    private static float distanceTo(EntityData a, EntityData b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private Color GetStarColor(double colorIndex)
    {
        // Implement your own logic for determining the star's color based on its color index
        // This is just a simple example
        if (colorIndex < -0.4)
            return Color.blue;
        else if (colorIndex < 0.0)
            return Color.white;
        else if (colorIndex < 0.4)
            return Color.yellow;
        else
            return Color.red;
    }
}



public class EntityData
{
    public string Name;
    public float X;
    public float Y;
    public float Z;
    public float Distance;
    public int ColorIndex;
    public Color Color;
}
