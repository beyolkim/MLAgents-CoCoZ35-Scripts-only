using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class ChickenAgent : Agent
{
    public Transform groundTr;
    public Transform goalTr;
    public GameObject RoosterOTC; //수탉 장애물

    //ray
    private RayPerception3D ray;
    private Transform tr;
    private Rigidbody rb;

    //광선의 거리
    public float rayDistance = 50.0f;
    //광선의 발사 각도 (7개의 광선)
    private float[] rayAngles = { 20.0f, 45.0f, 70.0f, 90.0f, 110.0f, 135.0f, 160.0f };
    //광선의 검출 대상 (검출 대상은 4개)
    private string[] detectObjects = { "DEADZONE", "MONSTER", "GOAL", "AGENT" };

    private Transform chickenTr; //agent
    private Transform parentObj;
    private GameObject GroundPrefab;
    private int X = 1;
    private int Y = 0;
    private bool check_Num;


    public void Awake()
    {
        parentObj = this.gameObject.transform.parent;
        GroundPrefab = Resources.Load<GameObject>("Ground");
    }
    public override void InitializeAgent() //reset되고 실행
    {
        chickenTr = GetComponent<Transform>(); //agent의 위치
        InvokeRepeating("CreateObstacle", 0.5f, 1.0f);  //2초뒤에 1.5초마다
        tr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody>();
        ray = GetComponent<RayPerception3D>();
    }
    void CreateObstacle()
    {
        float randomX = Random.Range(-1.5f, 1.5f);
        float randomZ = Random.Range(-4.0f, 4.0f);
       
        GameObject obstacle = (GameObject)Instantiate(RoosterOTC
                                                     , new Vector3(randomX + parentObj.position.x 
                                                     , 0.0f
                                                     , randomZ + parentObj.position.z + (10 * Y))
                                                     , Quaternion.Euler(0, 180, 0), parentObj);
        if(check_Num == true&& X==Y+2)
        {
            Y++;
        }

        obstacle.name = "Rooster"; //클론 이름
    }

    public override void CollectObservations()
    {
        //광선이 7개, 검출 대상이 4개다. 그런데 검출 대상안에 이미 플래그 값 2개가 있으므로 총 7 * (4 + 2) = 42개로 계산 해야한다
        AddVectorObs(ray.Perceive(rayDistance, rayAngles, detectObjects, 0.5f, 0.5f)); //마지막 숫자는 광선의 y값 (몬스터의 배쪽에 레이)

    

        //#1 관측정보
        //바닥의 중심과 에이전트의 거리
        Vector3 dist1 = groundTr.position - chickenTr.position;

        //(5.0f = 바닥의 가로 길이 /2) : 관측 데이터의 정규화(Normalized)
        float norX1 = Mathf.Clamp(dist1.x / 2.0f, -1.0f, +1.0f);
        float norZ1 = Mathf.Clamp(dist1.z / 5.0f, -1.0f, +1.0f);

        //#2 관측정보
        //에이전트와 목적지(Goal)간의 거리가 가까울 수록 + 보상을 받을 가능성이 커진다.
        Vector3 dist2 = goalTr.position - chickenTr.position;

        //Mathf.Clamp(어떤 값, -1.0f, +1.0f 사이로 고정된다)
        //이렇게 -1 ~ 1사이로 값을 고정해야 효율이 높아짐 = 벡터 노멀라이즈
        float norX2 = Mathf.Clamp(dist2.x / 2.0f, -1.0f, +1.0f);
        float norZ2 = Mathf.Clamp(dist2.z / 5.0f, -1.0f, +1.0f);

        //브레인에 관측 정보 전달
        AddVectorObs(norX1);
        AddVectorObs(norZ1);
        AddVectorObs(norX2);
        AddVectorObs(norZ2);


    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        float h = vectorAction[0];
        float v = vectorAction[1];

        //이동할 방향 벡터를 계산
        Vector3 dir = (Vector3.forward * v) + (Vector3.right * h);
        //에이전트 이동 처리
        chickenTr.Translate(dir * Time.deltaTime * 3.0f);
        if (dir.z < 0)
        {
            AddReward(-1.0f);
        }
        else if (dir.z > 0)
        {
            AddReward(+1.0f);
        }
        AddReward(-0.001f);
    }

    void OnTriggerEnter(Collider coll)
    {
        //병아리 구출 : + 보상
        if (coll.CompareTag("GOAL"))
        {
            AddReward(+5.0f);
            ResetStage();
        }

        //수탉에 충돌하면 : -보상
        if (coll.CompareTag("DEADZONE") || coll.CompareTag("MONSTER"))
        {
            AddReward(-2.0f);
            Done();
        }

        //Joint에 충돌하면 맵 생성
        if (coll.CompareTag("JOINT"))
        {
            Instantiate(GroundPrefab, new Vector3(0, -0.25f, -5 + (10 * X)), Quaternion.identity, parentObj);
            X++;
            if (X == 2)
            {
                check_Num = true;
            }
        }
    }
    public override void AgentReset()
    {
        ResetStage();
    }

    void ResetStage()
    {
        //에이전트의 위치를 변경
        chickenTr.localPosition = new Vector3(0.0f, 0.0f, -4.5f);

        //목적지의 위치를 변경
        goalTr.localPosition = new Vector3(0.0f, 0.0f, 35.0f);

        //RoosterOTC 리셋시 삭제
        foreach (var obj in parentObj.GetComponentsInChildren<Transform>())
        {
            if (obj.name == "Rooster")
            {
                Destroy(obj.gameObject);
            }
        }

        //맵 리셋시 삭제
        foreach (var obj2 in parentObj.GetComponentsInChildren<Transform>())
        {
            if (obj2.name == "Ground(Clone)")
            {
                Destroy(obj2.gameObject);
            }
        }

        //생기는 땅 위치 변경
        X = 1;
        Y = 0;
    }

}



