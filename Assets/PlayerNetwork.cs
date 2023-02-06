using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Transform spawnedObjectPrefab; // Трансформ для спавна сферы
    Transform spawnedObjectTransform;
    Vector3 moveDir;
    float moveSpeed;

    // Сетевая переменная. Тип указывается в <>, должен быть типом значения, а не ссылочным
    // Сыллочные - это строки, массивы, классы и так далее
    // Однако можно использовать struct и FixedString
    private NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>(
        new MyCustomData
        {
            _int = 56,
            _bool = true,
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public struct MyCustomData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        }
    }
    
    // Что-то типа дефолтного метода, по типу Awake или Start, но для мультиплеера
    // Awake или Start вроде не должен использоваться вообще в мультиплеере
    public override void OnNetworkSpawn()
    {
        // Чтение сетевой переменной при изменении её значения
        // => - лямбда выражение
        randomNumber.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
        {
            Debug.Log(OwnerClientId + "; " + newValue._int + "; " + newValue._bool + "; " + newValue.message);
        };
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;                               // Строка нужна, чтобы Update происходил на владельце 
                                                            // т.к. игкроков много и они все управляют одним префабом

        if (Input.GetKeyDown(KeyCode.T)) CreateObj();       // Создание сферы
        if (Input.GetKeyDown(KeyCode.Y)) DeleteObj();       // Удаление сферы

        Move();
    }

    public void CreateObj()
    {
        CreateServerRpc();      // Я не ебу почему именно так :)

                                // Ниже код для смены значения сетевой переменной

        /*
        randomNumber.Value = new MyCustomData
        {
            _int = 10,
            _bool = false,
            message = "BOOM",
        };
        */
    }
    
    public void DeleteObj()
    {
        DestroyServerRpc();     // Опять не ебу :)
    }

    public void Move()
    {
        moveDir = new Vector3(0, 0, 0);

        if(Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if(Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if(Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if(Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    // ServerRpc Отправляет данные на сервер. 

    // С этой штукой могут происходить странные вещи, например, если ты клиент и вызываешь эту функцию,
    // то её результат не факт что появится у тебя на экране, но появится у хоста/сервера
    // Т.к. эта функция происходит только у хоста, нужно компенсировать

    // Опять же, метод не может принимать в себя ссылочные типы
    // Но ServerRpc может принимать на вход строки (исключение типа)

    // Есть ещё ClientRpc,но я его не юзал
    // Прикол ClientRpc в том, что он может запускаться только на хосте/сервеере, а результат
    // отправляется на все клиенты
    [ServerRpc]
    private void CreateServerRpc()
    {
        spawnedObjectTransform = Instantiate(spawnedObjectPrefab);              // Обычный спавн локально
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);       // NetworkObject - скрипт, который навешен на сферу
    }
    [ServerRpc]
    private void DestroyServerRpc()
    {
        //spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);   // не помню чё это такое и почему закоменчено :)
        Destroy(spawnedObjectTransform.gameObject);
    }
}
