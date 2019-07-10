using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


public struct DataPoint
{
    public Transform Transform;
    public Vector3 Position;
    public DataPoint(Transform transform, Vector3 position)
    {
        Transform = transform;
        Position = position;
    }
}
public class Cluster
{
    public Vector3 m_centroid = Vector3.zero;
    public List<DataPoint> m_dataPoints = new List<DataPoint>();

    public Cluster()
    {
    }
    public Cluster(DataPoint dataPoint)
    {
        m_dataPoints.Add(dataPoint);
    }
    public Cluster(Cluster cluster1, Cluster cluster2)
    {
        m_dataPoints.AddRange(cluster1.m_dataPoints);
        m_dataPoints.AddRange(cluster2.m_dataPoints);
    }

    public Vector3 UpdatedCetroid()
    {
        if (m_dataPoints.Count == 0)
        {
            return m_centroid;
        }
        Vector3 centroid = new Vector3();
        for (int i = 0; i < m_dataPoints.Count; i++)
        {
            centroid += m_dataPoints[i].Position;
        }
        return centroid / m_dataPoints.Count;
    }

    public Vector3 Farest(Vector3 position)
    {
        Vector3 farest = m_dataPoints[0].Position;
        for (int i = 0; i < m_dataPoints.Count; i++)
        {
            if (Vector3.Distance(m_dataPoints[i].Position, position) > Vector3.Distance(farest, position))
                farest = m_dataPoints[i].Position;
        }
        return farest;
    }

    //Sum of Squared Error
    public float SSE()
    {
        float sse = 0;
        for (int i = 0; i < m_dataPoints.Count; i++)
        {
            sse += Mathf.Pow(Vector3.Distance(m_dataPoints[i].Position, m_centroid), 2);
        }
        return sse;
    }

}

public class ClusterManager : MonoBehaviour
{
    [Header("Prefabs")]
    public MeshRenderer m_ObjectPrefab;
    public MeshRenderer m_FloorPrefab;

    [Header("Settings")]
    [Range(1, 100)]
    public int m_BushDensity;

    [Range(10, 100)]
    public int m_BushSize;

    [Range(100, 500)]
    public int m_WorldSize;

    public float m_ClusterThresholdHAC = 50;

    [Range(0, 1)]
    public float m_ProgressionThreshold = 0.5f;

    [Range(0, 10)]
    public int m_DesiredClusterCount = 3;

    [Range(1, 10)]
    public int m_RandomCentroidChoices = 3;

    private List<Transform> mFloorInstances;
    private List<Transform> mObjectInstances;

    private Stopwatch mStopwatch;

    private List<Cluster> mClusters;

    private Action<List<Cluster>, List<Transform>> OnClustersUpdate;
    
    void Awake()
    {
        mStopwatch = new Stopwatch();
        mFloorInstances = new List<Transform>();
        mObjectInstances = new List<Transform>();

        InitializeFloor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Reset();
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                SpawnObjects(hit.point);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            mStopwatch.Restart();
            mClusters = HAC.Clusterize(mObjectInstances, m_ClusterThresholdHAC);
            mStopwatch.Stop();
            UnityEngine.Debug.LogFormat("Cluster Count: {2}. Instance Count: {0}. Clusterize Time: {1}", mObjectInstances.Count, mStopwatch.Elapsed, mClusters.Count);

            UnityEngine.Random.InitState(Time.frameCount);
            Colorize();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            mStopwatch.Restart();
            mClusters = KMeans.Clusterize(mObjectInstances, m_DesiredClusterCount, m_RandomCentroidChoices, m_ProgressionThreshold);
            mStopwatch.Stop();
            UnityEngine.Debug.LogFormat("Cluster Count: {2}. Instance Count: {0}. Clusterize Time: {1}", mObjectInstances.Count, mStopwatch.Elapsed, mClusters.Count);

            UnityEngine.Random.InitState(Time.frameCount);
            Colorize();

            if(OnClustersUpdate != null)
                OnClustersUpdate(mClusters, mFloorInstances);

            StopAllCoroutines();
          //  StartCoroutine(Clusterize());
        }
        if (Input.GetKeyDown(KeyCode.M))
            for (int i = 0; i < mObjectInstances.Count; i++)
            {
                Vector2 force = UnityEngine.Random.insideUnitCircle * 100;
                mObjectInstances[i].GetComponent<Rigidbody>().AddForce(new Vector3(force.x, 0, force.y));
            }
    }
    
    IEnumerator Clusterize()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            mClusters = KMeans.Clusterize(mObjectInstances, m_DesiredClusterCount, m_RandomCentroidChoices, m_ProgressionThreshold);
            Colorize();
        }
    }

    private void Colorize()
    {
        foreach (Cluster item in mClusters)
        {
            Color color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            foreach (DataPoint dataPoint in item.m_dataPoints)
            {
                dataPoint.Transform.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
            }
        }
    }
    private void Reset()
    {
        for (int i = 0; i < mFloorInstances.Count; i++)
        {
            Destroy(mFloorInstances[i].gameObject);
        }
        mFloorInstances?.Clear();

        for (int i = 0; i < mObjectInstances.Count; i++)
        {
            Destroy(mObjectInstances[i].gameObject);
        }
        mObjectInstances?.Clear();

        mClusters?.Clear();

        InitializeFloor();
    }

    private void InitializeFloor()
    {
        for (int i = -m_WorldSize; i < m_WorldSize; i += 100)
        {
            for (int j = -m_WorldSize; j < m_WorldSize; j += 100)
            {
                MeshRenderer newfloorchunck = Instantiate(m_FloorPrefab, new Vector3(i + 50, 0, j + 50), Quaternion.identity, transform);
                newfloorchunck.material.SetColor("_Color", UnityEngine.Random.ColorHSV(0.6f, 0.7f, 0.0f, 0.1f, 0.0f, 0.1f));

                mFloorInstances.Add(newfloorchunck.transform);
            }
        }
    }

    private void SpawnObjects(Vector3 bushCenter)
    {
        for (int i = 0; i < m_BushDensity; i++)
        {
            Vector3 position = bushCenter + new Vector3(UnityEngine.Random.Range(-1f, 1f) * m_BushSize, 10, UnityEngine.Random.Range(-1f, 1f) * m_BushSize);
            MeshRenderer newObject = Instantiate(m_ObjectPrefab, position, Quaternion.identity);
            newObject.material.SetColor("_Color", UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));

            mObjectInstances.Add(newObject.transform);
        }
    }

    public void RegisterClusterCallback(Action<List<Cluster>, List<Transform>> action)
    {
        OnClustersUpdate += action;
    }
    public void RemoveClusterCallback(Action<List<Cluster>, List<Transform>> action)
    {
        OnClustersUpdate -= action;
    }
}
