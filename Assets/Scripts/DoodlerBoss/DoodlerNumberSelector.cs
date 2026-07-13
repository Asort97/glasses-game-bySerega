using UnityEngine;

public class DoodlerNumberSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoodlerPlatformSystem platformSystem;
    // The left atlas places 2 above 1, so these names reflect the displayed numbers.
    [SerializeField] private GameObject numberOneSelected;
    [SerializeField] private GameObject numberTwoSelected;
    [SerializeField] private GameObject numberThreeSelected;
    [SerializeField] private GameObject numberFourSelected;

    [SerializeField, Range(1, 4)] private int initialNumber = 1;

    private int _selectedNumber;

    private void OnEnable()
    {
        SelectNumber(initialNumber);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveRight();
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLeft();
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            MoveDown();
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            MoveUp();
    }

    private void MoveRight()
    {
        if (_selectedNumber == 2)
            SelectNumber(3);
        else if (_selectedNumber == 1)
            SelectNumber(4);
    }

    private void MoveLeft()
    {
        if (_selectedNumber == 3)
            SelectNumber(2);
        else if (_selectedNumber == 4)
            SelectNumber(1);
    }

    private void MoveDown()
    {
        if (_selectedNumber == 2)
            SelectNumber(1);
        else if (_selectedNumber == 3)
            SelectNumber(4);
    }

    private void MoveUp()
    {
        if (_selectedNumber == 1)
            SelectNumber(2);
        else if (_selectedNumber == 4)
            SelectNumber(3);
    }

    private void SelectNumber(int number)
    {
        _selectedNumber = number;

        numberOneSelected.SetActive(number == 1);
        numberTwoSelected.SetActive(number == 2);
        numberThreeSelected.SetActive(number == 3);
        numberFourSelected.SetActive(number == 4);

        platformSystem.SetSelectedNumber(_selectedNumber);
    }
}
