using System.Collections.Generic;
using UnityEngine;

public class MoraMinigameManager : MonoBehaviour
{
    private int emptyIndex = 8;
    private int chosenSetIndex;
    private bool gameEnded = false;

    [SerializeField] private List<SpriteRenderer> squares;
    [SerializeField] private List<TileSet> tileSets;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject giveUpPanel;
    [SerializeField] private GameObject gameBoard;

    private TileSet currentSet;

    private void Start()
    {
        winPanel.SetActive(false);
        giveUpPanel.SetActive(false);
        gameBoard.SetActive(true);

        AssignOriginal();
        Invoke(nameof(AssignShuffled), 1.5f);
    }

    private void Update()
    {
        if (gameEnded) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null)
            {
                int clickedIndex = squares.FindIndex(sq => sq.gameObject == hit.gameObject);
                if (clickedIndex != -1 && IsAdjacent(clickedIndex, emptyIndex))
                {
                    MoveTile(clickedIndex);
                    if (CheckWin())
                    {
                        gameEnded = true;
                        gameBoard.SetActive(false);
                        winPanel.SetActive(true);
                    }
                }
            }
        }
    }

    private void AssignOriginal()
    {
        chosenSetIndex = Random.Range(0, tileSets.Count);
        currentSet = tileSets[chosenSetIndex];

        for (int i = 0; i < squares.Count; i++)
        {
            squares[i].sprite = currentSet.tiles[i];
            FitSpriteToSquare(squares[i]);
            ResizeCollider(squares[i]);
        }
    }

    private void AssignShuffled()
    {
        List<Sprite> shuffledTiles = new(currentSet.tiles);
        Shuffle(shuffledTiles);
        shuffledTiles[8] = null;

        for (int i = 0; i < squares.Count; i++)
        {
            squares[i].sprite = shuffledTiles[i];
            FitSpriteToSquare(squares[i]);
            ResizeCollider(squares[i]);
        }

        emptyIndex = 8;
    }

    private void MoveTile(int clickedIndex)
    {
        squares[emptyIndex].sprite = squares[clickedIndex].sprite;
        squares[clickedIndex].sprite = null;

        FitSpriteToSquare(squares[emptyIndex]);
        FitSpriteToSquare(squares[clickedIndex]);
        ResizeCollider(squares[emptyIndex]);
        ResizeCollider(squares[clickedIndex]);

        emptyIndex = clickedIndex;
    }

    private bool IsAdjacent(int a, int b)
    {
        int rowA = a / 3, colA = a % 3;
        int rowB = b / 3, colB = b % 3;
        return Mathf.Abs(rowA - rowB) + Mathf.Abs(colA - colB) == 1;
    }

    private void Shuffle(List<Sprite> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    private void FitSpriteToSquare(SpriteRenderer sr)
    {
        if (sr.sprite == null) return;

        float targetSize = 2f;
        float pixelsPerUnit = sr.sprite.pixelsPerUnit;
        float spriteWidth = sr.sprite.rect.width;

        float scale = targetSize / (spriteWidth / pixelsPerUnit);
        sr.transform.localScale = new Vector3(scale, scale, 1);
    }

    private void ResizeCollider(SpriteRenderer sr)
    {
        BoxCollider2D col = sr.GetComponent<BoxCollider2D>();
        if (col == null) return;

        if (sr.sprite != null)
        {
            col.size = sr.sprite.bounds.size;
            col.offset = sr.sprite.bounds.center;
        }
        else
        {
            col.size = Vector2.zero;
            col.offset = Vector2.zero;
        }
    }

    private bool CheckWin()
    {
        for (int i = 0; i < 8; i++)
        {
            if (squares[i].sprite == null || squares[i].sprite.name != currentSet.tiles[i].name)
                return false;
        }
        return squares[8].sprite == null;
    }

    public void RestartGame()
    {
        winPanel.SetActive(false);
        giveUpPanel.SetActive(false);
        gameEnded = false;
        gameBoard.SetActive(true);
        AssignOriginal();
        Invoke(nameof(AssignShuffled), 1.5f);
    }

    public void GiveUp()
    {
        gameEnded = true;
        giveUpPanel.SetActive(true);
        gameBoard.SetActive(false);
    }
}