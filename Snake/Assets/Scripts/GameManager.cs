using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SA
{
    public class GameManager : MonoBehaviour
    {
        public int maxHeight = 15;
        public int maxWidth = 15;

        public Color color1;
        public Color color2;
        public Color playerColor;

        private GameObject playerObj;
        private Node playerNode;

        private GameObject map;
        private SpriteRenderer mapRenderer;
        public Transform cameraHolder;

        private Node[,] grid;
        private bool up, down, left, right;
        private bool movePlayer;

        private Direction currentDirection;

        public enum Direction
        {
            up, down, left, right
        }
        private void Start()
        {
            CreateMap();
            PlacePlayer();
            PlaceCamera();
        }

        private void Update()
        {
            GetInput();
            SetPlayerDirection();
            MovePlayer();
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
            {
                currentDirection = Direction.up;
                movePlayer = true;
            }
            else if (down)
            {
                currentDirection = Direction.down;
                movePlayer = true;
            }
            else if (left)
            {
                currentDirection = Direction.left;
                movePlayer = true;
            }
            else if (right)
            {
                currentDirection = Direction.right;
                movePlayer = true;
            }
        }

        private void MovePlayer()
        {
            if (!movePlayer)
                return;

            movePlayer = false;

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

            Node targetNode = GetNode(playerNode.x + x, playerNode.y + y);
            if (targetNode == null)
            {

            }
            else
            {
                playerObj.transform.position = targetNode.worldPosition;
                playerNode = targetNode;
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
            return Sprite.Create(texture2D, rect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
        }

        private void PlacePlayer()
        {
            playerObj = new GameObject("Player 1");
            SpriteRenderer playerRenderer = playerObj.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = CreateSprite(playerColor);
            playerRenderer.sortingOrder = 1;
            playerNode = GetNode(3, 3);
            playerObj.transform.position = playerNode.worldPosition;
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
    }
}
