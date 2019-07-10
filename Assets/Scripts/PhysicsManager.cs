using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PhysicsManager : MonoBehaviour
{
    List<PhysicsScene> physicsScenes;
    List<Scene> scenes;

    ClusterManager clusterManager;

    List<Cluster> mClusters;
    void Awake()
    {
        clusterManager = GetComponent<ClusterManager>();
        clusterManager.RegisterClusterCallback(OnUpdateClusters);

        physicsScenes = new List<PhysicsScene>();
        scenes = new List<Scene>();

        CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        for (int i = 0; i < 5; i++)
        {
            Scene s = SceneManager.CreateScene("Physics Scene " + (i + 1), csp);
            scenes.Add(s);
            physicsScenes.Add(s.GetPhysicsScene());
        }
    }
    private void OnDestroy()
    {
        clusterManager.RemoveClusterCallback(OnUpdateClusters);
    }

    void FixedUpdate()
    {
        MoveSceneCenterToOrigin();
        for (int i = 0; i < 5; i++)
        {
            physicsScenes[i].Simulate(Time.deltaTime);
        }
        MoveSceneCenterToClusterCentroid();
    }

    void MoveSceneCenterToOrigin()
    {
        if (mClusters == null) return;

        for (int i = 0; i < mClusters.Count; i++)
        { 
            for (int j = 0; j < mClusters[i].m_dataPoints.Count; j++)
            {
                mClusters[i].m_dataPoints[j].Transform.position -= mClusters[i].m_centroid;
            }
        }
    }

    void MoveSceneCenterToClusterCentroid()
    {
        if (mClusters == null) return;

        for (int i = 0; i < mClusters.Count; i++)
        {
            for (int j = 0; j < mClusters[i].m_dataPoints.Count; j++)
            {
                mClusters[i].m_dataPoints[j].Transform.position += mClusters[i].m_centroid;
            }
        }

    }

    private void OnUpdateClusters(List<Cluster> clusters, List<Transform> floor)
    {
        mClusters = clusters;

        for (int i = 0; i < floor.Count; i++)
        {
            for (int j = 0; j < clusters.Count; j++)
            {
                GameObject newFloor = Instantiate(floor[i].gameObject);
                clusters[j].m_dataPoints.Add(new DataPoint(newFloor.transform, Vector3.zero));
                SceneManager.MoveGameObjectToScene(newFloor, scenes[j]);
            }

        }
        for (int i = 0; i < clusters.Count; i++)
        {
            for (int j = 0; j < clusters[i].m_dataPoints.Count; j++)
            {
                SceneManager.MoveGameObjectToScene(clusters[i].m_dataPoints[j].Transform.gameObject, scenes[i]);
            }
        }
    }
}
