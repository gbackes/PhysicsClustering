using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Index
{
    public int x;
    public int y;
    
    public Index(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
public class DistanceMatrix
{
    public List<Cluster> m_clusters;
    public float[,] m_matrix;

    Index m_minIndex;
    public float MinDistance {
        get
        {
            return m_matrix[m_minIndex.x, m_minIndex.y];
        }
    }

    public DistanceMatrix(List<Transform> objects)
    {
        List<DataPoint> dataPoints = KMeans.NormalizeData(objects);
        m_matrix = new float[dataPoints.Count, dataPoints.Count];
        m_clusters = new List<Cluster>();
        m_minIndex = new Index(0, 0);

        for (int i = 0; i < dataPoints.Count; i++)
        {
            m_clusters.Add(new Cluster(dataPoints[i]));

            for (int j = i; j < dataPoints.Count; j++)
            {
                if (i == j)
                    m_matrix[i, j] = -1;
                else
                    m_matrix[i, j] = m_matrix[j, i] = Vector3.Distance(dataPoints[i].Position, dataPoints[j].Position);
            }
        }
        UpdateMin();
    }
    
    void UpdateMin()
    {
        m_minIndex.x = 0;
        m_minIndex.y = m_clusters.Count - 1;
        for (int i = 0; i < m_clusters.Count; i++)
        {
            for (int j = i + 1; j < m_clusters.Count; j++)
            {
                if (m_matrix[i, j] < MinDistance)
                {
                    m_minIndex.x = i;
                    m_minIndex.y = j;
                }
            }
        }
    }

    public void MergeClusters()
    {
        Cluster newCluster = new Cluster(m_clusters[m_minIndex.x], m_clusters[m_minIndex.y]);

        RebuildMatrix();

        if(m_minIndex.x > m_minIndex.y)
        {
            m_clusters.RemoveAt(m_minIndex.x);
            m_clusters.RemoveAt(m_minIndex.y);
        }else
        {
            m_clusters.RemoveAt(m_minIndex.y);
            m_clusters.RemoveAt(m_minIndex.x);
        }

        m_clusters.Add(newCluster);

        UpdateMin();
    }

    void RebuildMatrix()
    {
        float[,] newMatrix = new float[m_clusters.Count - 1, m_clusters.Count - 1];

        Index currentIndex = new Index(0, 0);
        for (int i = 0; i < m_clusters.Count; i++)
        {
            if (i == m_minIndex.x || i == m_minIndex.y) continue;

            currentIndex.y = currentIndex.x;
            for (int j = i; j < m_clusters.Count; j++)
            {
                if (j == m_minIndex.x || j == m_minIndex.y) continue;

                newMatrix[currentIndex.x, currentIndex.y] = newMatrix[currentIndex.y, currentIndex.x] = m_matrix[i, j];

                currentIndex.y++;
            }

            newMatrix[m_clusters.Count - 2, currentIndex.x] = newMatrix[currentIndex.x, m_clusters.Count - 2] = Mathf.Max(m_matrix[i, m_minIndex.x], m_matrix[i, m_minIndex.y]);
            currentIndex.x++;
        }

        m_matrix = newMatrix;
    }
}

/// <summary>
/// HIERARCHICAL AGGLOMERATIVE CLUSTERING
/// </summary>
public static class HAC
{
    public static List<Cluster> Clusterize(List<Transform> objects, float threshold)
    {
        DistanceMatrix distanceMatrix = new DistanceMatrix(objects);

        while (distanceMatrix.MinDistance < threshold && distanceMatrix.m_clusters.Count > 1)
        {
            distanceMatrix.MergeClusters();
        }

        return distanceMatrix.m_clusters;
    }
}
