using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DolbyIO.Comms;
using DolbyIO.Comms.Unity;

public class ButtonScript : MonoBehaviour
{
    public GameObject progressIndicator; // Reference to the join progress indicator panel
    public TextMeshProUGUI progressText; // Reference to the TextMeshProUGUI component
    public Button joinButton; // Reference to the join button
    public Button leaveButton; // Reference to the leave button

    public TestConferenceController TestConferenceController; // Reference to the TestConferenceController script
    private static bool isConnected = false; // Flag to track if the user is already connected

    public void OnJoinButtonClick()
    {
        if (!isConnected)
        {
            // Disable the button
            joinButton.interactable = false;

            // Show the progress indicator
            progressIndicator.SetActive(true);
            progressText.text = "Connecting...";

            // Simulate connecting to the game (replace with your actual connection logic)
            StartCoroutine(ConnectToConference());
        }
    }

    private IEnumerator ConnectToConference()
    {
        yield return TestConferenceController.JoinCoroutine();

        // Connection successful
        progressText.text = "Connected!";
        isConnected = true;

        // Hide the progress indicator
        yield return new WaitForSeconds(1);
        progressIndicator.SetActive(false);

        // Enable the button again
        joinButton.interactable = true;
    }

    public void OnLeaveButtonClick()
    {
        if (isConnected)
        {
            // Disable the button
            GetComponent<Button>().interactable = false;

            // Show the progress indicator
            progressIndicator.SetActive(true);
            progressText.text = "Disconnecting...";

            // Simulate connecting to the game (replace with your actual connection logic)
            StartCoroutine(Disconnect());
        }
    }

    private IEnumerator Disconnect()
    {
        yield return TestConferenceController.LeaveCoroutine();

        // Disconnection successful
        progressText.text = "Disconnected!";
        isConnected = false;

        // Hide the progress indicator
        yield return new WaitForSeconds(1);
        progressIndicator.SetActive(false);

        // Enable the button again
        leaveButton.interactable = true;
    }
}
