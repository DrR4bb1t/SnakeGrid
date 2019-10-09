using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;

namespace SA
{
    public class GameManager : NetworkBehavior
    {
        private int maxHeight = 8;
        private int maxWidth = 8;

        public Color color1;
        public Color color2;
        public Color player1Color;
        public Color player2Color;
        public Color foodColor;
        public Color powerUpColor;

        private GameObject player1;
        private GameObject player2;
        private GameObject food;
        private GameObject powerUp;
        private GameObject tailParent1;
        private GameObject tailParent2;
        private Node player1Node;
        private Node player2Node;
        private Node prevPlayerNode;
        private Node foodNode;
        private Node powerUpNode;
        private Sprite player1Sprite;
        private Sprite player2Sprite;

        private GameObject map;
        private SpriteRenderer mapRenderer;

        [SerializeField]
        private Transform cameraHolder;

        private Node[,] grid;
        private List<Node> availableNodes = new List<Node>();
        List<SpecialNode> tail = new List<SpecialNode>();
        private bool up, down, left, right;

        private Direction targetDirection;
        private Direction currentDirection;

        private bool isGamerOver = false;
        private bool isFirstInput = false;
        private bool FlashModeIsActive = false;
        private float upTimeCounter = 3f;
        private bool foodEaten = false;
        private bool isCoolingDown = false;
        private bool gameStarted = false;

        private float moveRate = 0.65f;
        private float timer;
        private float cooldown = 5f;

        private int currentScore1;
        private int highScore1;
        private int currentScore2;
        private int highScore2;

        public enum Direction
        {
            up, down, left, right
        }

        public UnityEvent onStart;
        public UnityEvent onGameOver;
        public UnityEvent firstInput;
        public UnityEvent onScore;

        public Text currentScoreText;
        public Text highScoreText;
        private bool powerUpSpawned;

        private void Start()
        {
            onStart.Invoke();
            //StartNewGame();
        }

        public void StartNewGame()
        {
            ClearReferences();
            CreateMap();
            PlacePlayer1();
            PlaceCamera();
            foodEaten = true;
            CreateFood();
            targetDirection = Direction.right;
            isGamerOver = false;
            powerUpSpawned = false;
            currentScore1 = 0;
            cooldown = 5f;
            UpdateScore();
        }

        public void GameOver()
        {
            isGamerOver = true;
            isFirstInput = false;
        }

        public void ClearReferences()
        {
            if (map != null)
                Destroy(map);
            if (player1 != null)
                Destroy(player1);
            if (food != null)
                Destroy(food);
            if (powerUp != null)
                Destroy(powerUp);
            if (tailParent1 != null)
                Destroy(tailParent1);

            foreach (var t in tail)
            {
                if (t.obj != null)
                    Destroy(t.obj);
            }
            tail.Clear();
            availableNodes.Clear();
            grid = null;
        }

        private void Update()
        {
            if (isGamerOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                    onStart.Invoke();

                return;
            }

            gameStarted = true;
            GetInput();

            if (powerUpSpawned == false)
                Countdown();


            if (FlashModeIsActive == true)
                UpTimeOfFlashmode();

            //if (isFirstInput)
            //{

                SetPlayerDirection();
                UpdateScore();

                timer += Time.deltaTime;
                if (timer > moveRate)
                {
                    timer = 0;
                    currentDirection = targetDirection;
                    MovePlayer();

                }
            //}
            //else
            //{
            //    if (up || down || left || right)
            //    {
            //        isFirstInput = true;
            //        //firstInput.Invoke();
            //    }
            //}
        }

        private void GetInput()
        {
            up = Input.GetKeyDown(KeyCode.W);
            down = Input.GetKeyDown(KeyCode.S);
            left = Input.GetKeyDown(KeyCode.A);
            right = Input.GetKeyDown(KeyCode.D);
        }

        private void SetPlayerDirection()
        {
            if (up)
                SetDirection(Direction.up);
            else if (down)
                SetDirection(Direction.down);
            else if (left)
                SetDirection(Direction.left);
            else if (right)
                SetDirection(Direction.right);
        }

        private void MovePlayer()
        {


            int x = 0;
            int y = 0;

            switch (currentDirection)
            {
                case Direction.up:
                    y = 1;
                    break;
                case Direction.down:
                    y = -1;
                    break;
                case Direction.left:
                    x = -1;
                    break;
                case Direction.right:
                    x = 1;
                    break;
                default:
                    break;
            }

            Node targetNode = GetNode(player2Node.x + x, player2Node.y + y);
            if (targetNode == null)
            {
                onGameOver.Invoke();
            }
            else
            {
                if (IsTailNode(targetNode))
                {
                    onGameOver.Invoke();
                }
                else
                {


                    bool isScore = false;

                    if (targetNode == foodNode)
                    {
                        isScore = true;
                        foodEaten = true;
                    }

                    if (targetNode == powerUpNode)
                    {
                        EnterFlashmode();
                    }

                    Node previousNode = player2Node;
                    availableNodes.Add(previousNode);


                    if (isScore)
                    {
                        tail.Add(CreateTailNode(previousNode.x, previousNode.y));
                        availableNodes.Remove(previousNode);
                    }

                    MoveTail();

                    //if (FlashModeIsActive == false)
                    //    RandomlyPlaceAndPowerUp();

                    PlacePlayerObject(player1, targetNode.worldPosition);
                    player2Node = targetNode;
                    availableNodes.Remove(player2Node);
                    if (isScore)
                    {
                        currentScore1++;
                        if (currentScore1 >= highScore1)
                            highScore1 = currentScore1;

                        onScore.Invoke();

                        if (availableNodes.Count > 0)
                            RandomlyPlaceAndPowerUp();
                    }
                }

            }
        }

        private void CreateMap()
        {
            map = new GameObject("Grid");
            mapRenderer = map.AddComponent<SpriteRenderer>();

            grid = new Node[maxWidth, maxHeight];

            Texture2D texture2D = new Texture2D(maxWidth, maxHeight);
            for (int x = 0; x < maxWidth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    Vector3 tp = Vector3.zero;
                    tp.x = x;
                    tp.y = y;

                    Node n = new Node()
                    {
                        x = x,
                        y = y,
                        worldPosition = tp
                    };

                    grid[x, y] = n;
                    availableNodes.Add(n);

                    if (x % 2 != 0)
                    {
                        if (y % 2 != 0)
                            texture2D.SetPixel(x, y, color1);
                        else
                            texture2D.SetPixel(x, y, color2);
                    }
                    else
                    {
                        if (y % 2 != 0)
                            texture2D.SetPixel(x, y, color2);
                        else
                            texture2D.SetPixel(x, y, color1);
                    }

                }
            }
            texture2D.filterMode = FilterMode.Point;

            texture2D.Apply();
            Rect rect = new Rect(0, 0, maxWidth, maxHeight);
            Sprite sprite = Sprite.Create(texture2D, rect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            mapRenderer.sprite = sprite;
        }

        private Sprite CreateSprite(Color targetColor)
        {
            Texture2D texture2D = new Texture2D(1, 1);

            texture2D.SetPixel(0, 0, targetColor);
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            Rect rect = new Rect(0, 0, 1, 1);
            return Sprite.Create(texture2D, rect, Vector2.one * 0.5f, 1, 0, SpriteMeshType.FullRect);
        }

        private void PlacePlayer1()
        {
            player1 = new GameObject("Player 1");
            SpriteRenderer player1Renderer = player1.AddComponent<SpriteRenderer>();

            player1Sprite = CreateSprite(player1Color);
            player1Renderer.sprite = player1Sprite;
            player1Renderer.sortingOrder = 1;
            player2Node = GetNode(0, 7);
            PlacePlayerObject(player1, player2Node.worldPosition);
            player1.transform.localScale = Vector3.one * 1.1f;

            tailParent1 = new GameObject("TailParent");
        }

        private void PlacePlayer2()
        {
            player2 = new GameObject("Player 2");
            SpriteRenderer player2Renderer = player2.AddComponent<SpriteRenderer>();
            player2Sprite = CreateSprite(player2Color);
            player2Renderer.sprite = player2Sprite;
            player2Renderer.sortingOrder = 1;
            player2Node = GetNode(7, 0);
            PlacePlayerObject(player2, player2Node.worldPosition);
            player1.transform.localScale = Vector3.one * 1.1f;

            tailParent2 = new GameObject("TailParent");
        }

        private void CreateFood()
        {
            food = new GameObject("Food");
            SpriteRenderer foodRenderer = food.AddComponent<SpriteRenderer>();
            foodRenderer.sprite = CreateSprite(foodColor);
            foodRenderer.sortingOrder = 1;
            RandomlyPlaceAndPowerUp();
        }

        private void CreatePowerUp()
        {
            powerUp = new GameObject("Flash");
            SpriteRenderer powerUpRenderer = powerUp.AddComponent<SpriteRenderer>();
            powerUpRenderer.sprite = CreateSprite(powerUpColor);
            powerUpRenderer.sortingOrder = 1;
            RandomlyPlaceAndPowerUp();
        }

        private void RandomlyPlaceAndPowerUp()
        {
            int rng = Random.Range(0, availableNodes.Count);
            Node n = availableNodes[rng];


            if (foodEaten == true)
            {
                PlacePlayerObject(food, n.worldPosition);
                foodNode = n;
                foodEaten = false;
            }

            if (gameStarted == true)
            {
                if (powerUpSpawned == false && isCoolingDown == true)
                {
                    PlacePlayerObject(powerUp, n.worldPosition);
                    powerUpNode = n;

                    if (foodNode == powerUpNode)
                    {
                        rng = Random.Range(0, availableNodes.Count);
                        n = availableNodes[rng];
                        PlacePlayerObject(powerUp, n.worldPosition);
                        powerUpNode = n;
                    }

                    powerUpSpawned = true;
                    isCoolingDown = false;
                }
            }
        }

        private Node GetNode(int x, int y)
        {
            if (x < 0 || x > maxWidth - 1 || y < 0 || y > maxHeight - 1)
                return null;

            return grid[x, y];
        }

        private void PlaceCamera()
        {
            Node n = GetNode(maxWidth / 2, maxHeight / 2);
            cameraHolder.position = n.worldPosition;
        }

        private SpecialNode CreateTailNode(int x, int y)
        {
            SpecialNode s = new SpecialNode();
            s.node = GetNode(x, y);
            s.obj = new GameObject();
            s.obj.transform.parent = tailParent1.transform;
            s.obj.transform.position = s.node.worldPosition;
            s.obj.transform.localScale = Vector3.one * 0.85f;
            SpriteRenderer renderer = s.obj.AddComponent<SpriteRenderer>();
            renderer.sprite = player1Sprite;
            renderer.sortingOrder = 1;

            return s;
        }

        private void PlacePlayerObject(GameObject gameObject, Vector3 position)
        {
            position += Vector3.one * 0.5f;
            gameObject.transform.position = position;
        }

        private void MoveTail()
        {
            Node prevNode = null;

            for (int i = 0; i < tail.Count; i++)
            {
                SpecialNode p = tail[i];
                availableNodes.Add(p.node);

                if (i == 0)
                {
                    prevNode = p.node;
                    p.node = player2Node;
                }
                else
                {
                    Node prev = p.node;
                    p.node = prevNode;
                    prevNode = prev;
                }

                availableNodes.Remove(p.node);
                PlacePlayerObject(p.obj, p.node.worldPosition);
            }
        }

        private bool IsOpposite(Direction targetDirection)
        {
            switch (targetDirection)
            {
                default:
                case Direction.up:
                    if (currentDirection == Direction.down)
                        return true;
                    else
                        return false;
                case Direction.down:
                    if (currentDirection == Direction.up)
                        return true;
                    else
                        return false;
                case Direction.left:
                    if (currentDirection == Direction.right)
                        return true;
                    else
                        return false;
                case Direction.right:
                    if (currentDirection == Direction.left)
                        return true;
                    else
                        return false;
            }
        }

        private bool IsTailNode(Node n)
        {
            for (int i = 0; i < tail.Count; i++)
            {
                if (tail[i].node == n)
                    return true;
            }

            return false;
        }

        private void SetDirection(Direction d)
        {
            if (!IsOpposite(d))
                targetDirection = d;
        }

        public void UpdateScore()
        {
            currentScoreText.text = currentScore1.ToString();
            highScoreText.text = highScore1.ToString();
        }

        private void Countdown()
        {
            if (cooldown > 0)
            {
                isCoolingDown = true;
                cooldown -= Time.deltaTime;
            }
            else
            {
                CreatePowerUp();
                RandomlyPlaceAndPowerUp();
                powerUpSpawned = true;
            }
        }

        private void EnterFlashmode()
        {
            moveRate = 0.45f;
            FlashModeIsActive = true;
            upTimeCounter = 3f;
            Destroy(powerUp);
        }

        private void UpTimeOfFlashmode()
        {
            if (upTimeCounter > 0)
            {
                upTimeCounter -= Time.deltaTime;
                Debug.Log(upTimeCounter);
            }
            else
            {
                moveRate = 0.65f;
                FlashModeIsActive = false;
                cooldown = 5f;
                powerUpSpawned = false;
            }
        }
    }
}
