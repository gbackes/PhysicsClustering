using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ClusterConfiguration
{
    public List<Cluster> m_clusters;
    private float _SSE = -1;
    public float SSE
    {
        get
        {
            if (_SSE == -1)
                _SSE = CalculateSSE();

            return _SSE;
        }
    }

    public ClusterConfiguration(List<Cluster> clusters)
    {
        m_clusters = clusters;
        CalculateSSE();
    }
    private float CalculateSSE()
    {
        float sse = 0;
        for (int i = 0; i < m_clusters.Count; i++)
        {
            sse += m_clusters[i].SSE();
        }
        return sse;
    }
}
public class KMeans
{

    public static List<Cluster> Clusterize(List<Transform> objects, int clusterCount, int randomCentroidChoices, float progressionThreshold)
    {
        List<DataPoint> dataPoints = NormalizeData(objects);

        if (clusterCount > 0)
            return ClusterizeNRandom(dataPoints, clusterCount, randomCentroidChoices).m_clusters;
        else
        {
            clusterCount = 1;
            List<ClusterConfiguration> kClusters = new List<ClusterConfiguration>();

            for (int i = 0; ; i++, clusterCount++)
            {
                kClusters.Add(ClusterizeNRandom(dataPoints, clusterCount, randomCentroidChoices));

                if (i != 0 && (kClusters[i].SSE) > (kClusters[i - 1].SSE * progressionThreshold))
                    return kClusters[i - 1].m_clusters;
            }
        }
    }

    public static List<DataPoint> NormalizeData(List<Transform> objects)
    {
        List<DataPoint> dataPoints = new List<DataPoint>();
        Vector3 lowerBound = new Vector3(float.MaxValue, 1, float.MaxValue), upperBound = new Vector3(float.MinValue, 1, float.MinValue);
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].position.x < lowerBound.x)
            {
                lowerBound.x = objects[i].position.x;
            }
            if (objects[i].position.z < lowerBound.z)
            {
                lowerBound.z = objects[i].position.z;
            }
            if (objects[i].position.x > upperBound.x)
            {
                upperBound.x = objects[i].position.x;
            }
            if (objects[i].position.z > upperBound.z)
            {
                upperBound.z = objects[i].position.z;
            }
        }
        Vector3 extends = upperBound - lowerBound;
        extends.x = 1 / extends.x;
        extends.y = 1 / extends.y;
        extends.z = 1 / extends.z;
        for (int i = 0; i < objects.Count; i++)
        {
            Vector3 normalizedPosition = Vector3.Scale(objects[i].position - lowerBound, extends);
            normalizedPosition.y = 1;
            dataPoints.Add(new DataPoint(objects[i], normalizedPosition));
        }

        return dataPoints;
    }

    private static ClusterConfiguration ClusterizeNRandom(List<DataPoint> dataPoints, int clusterCount, int randomCentroidChoices)
    {
        List<ClusterConfiguration> availableChoices = new List<ClusterConfiguration>();
        for (int j = 0; j < randomCentroidChoices; j++)
        {
            availableChoices.Add(new ClusterConfiguration(ClusterizeK(dataPoints, clusterCount, j)));
        }
        return SelectOptimal(availableChoices);
    }

    private static ClusterConfiguration SelectOptimal(List<ClusterConfiguration> configurations)
    {
        ClusterConfiguration optimalClusterConfiguration = configurations[0];
        for (int i = 1; i < configurations.Count; i++)
        {
            if (configurations[i].SSE < optimalClusterConfiguration.SSE)
                optimalClusterConfiguration = configurations[i];
        }
        return optimalClusterConfiguration;
    }

    private static List<Cluster> ClusterizeK(List<DataPoint> dataPoints, int clusterCount, int seed)
    {
        List<Cluster> clusters = new List<Cluster>();

        for (int i = 0; i < clusterCount; i++)
        {
            clusters.Add(new Cluster());
        }
        
        Random.InitState(seed);
        Vector3 randomPosition;
        for (int i = 0; i < clusterCount; i++)
        {
            do  randomPosition = dataPoints[Random.Range(0, dataPoints.Count - 1)].Position;
                while (clusters.Exists(c => c.m_centroid == randomPosition));

            clusters[i].m_centroid = randomPosition;
        }

        bool recalculate;
        do
        {
            recalculate = false;

            for (int j = 0; j < clusterCount; j++)
            {
                clusters[j].m_dataPoints.Clear();
            }

            for (int i = 0; i < dataPoints.Count; i++)
            {
                Cluster closestCluster = clusters[0];
                for (int j = 0; j < clusterCount; j++)
                {
                    if (Vector3.Distance(dataPoints[i].Position, clusters[j].m_centroid) < Vector3.Distance(dataPoints[i].Position, closestCluster.m_centroid))
                        closestCluster = clusters[j];
                }
                closestCluster.m_dataPoints.Add(dataPoints[i]);
            }

            for (int j = 0; j < clusterCount; j++)
            {
                Vector3 currentCentroid = clusters[j].UpdatedCetroid();
                if (clusters[j].m_centroid != currentCentroid)
                {
                    clusters[j].m_centroid = currentCentroid;
                    recalculate = true;
                }
            }
        } while (recalculate);

        return clusters;
    }




}
