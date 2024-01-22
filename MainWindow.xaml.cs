using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace batch_audio_extractor
{
	/// <summary>
	/// A simple enum to define a track selection. The selection can either be track one, track two, or both tracks.
	/// </summary>
	public enum Track
	{
		Both,
		One,
		Two
	}


	/// <summary>
	/// Interaction logic for MainWindow.xaml in combination with an audio extractor.
	/// </summary>
	public partial class MainWindow : Window
	{
		// Constants related to sample rates and audio channels.
		private static readonly int DEFAULT_SAMPLE_RATE_SELECTION = 48000;
		private static readonly int MONO_CHANNEL_SELECTION = 1;
		private static readonly int STEREO_CHANNEL_SELECTION = 2;
		
		private List<string> input_file_list;
		private string? output_folder;
		private int audio_channel_selection;
		private int sample_rate_selection;
		private Track track_selection;
		
		/// <summary>
		/// MainWindow constructor. Initializes the window and instantiates the member variables of the class.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			// Instantiate memeber variables.
			FileListBox.Items.Add("No files selected.");
			input_file_list = new List<string>();
			output_folder = null;
			audio_channel_selection = STEREO_CHANNEL_SELECTION;
			sample_rate_selection = DEFAULT_SAMPLE_RATE_SELECTION;
			track_selection = Track.Both;
		}

		/// <summary>
		/// An event listener for when the browse button UI element is clicked.
		/// Prompts the user to select a folder as either an input or output directory. The selected folder is displayed in the UI.
		/// When selecting an input folder, all .mp4 files in the directory will also be displayed in the UI.
		/// </summary>
		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
            // Open a folder picker dialog.
			var dialog = new OpenFolderDialog();

			if (dialog.ShowDialog() == false)
			{
				return;
			}
			var selectedFolder = dialog.FolderName;

			// Determine if this folder picker dialog was created via the input or output button.
			if (sender.Equals(InputBrowseButton))
			{
				// Clear the file list box.
				FileListBox.Items.Clear();

				try
				{
					// Get all the .mp4 files from the picked folder and populate the file list box.
					input_file_list = Directory.GetFiles(selectedFolder, "*.mp4").ToList();
					foreach (var file in input_file_list)
					{
						FileListBox.Items.Add(file);
					}

					// If the file list box is empty we will put a message in it.
					if (!FileListBox.HasItems)
					{
						FileListBox.Items.Add("No files selected.");
					}

					// Set the input box to the folder name.
					InputTextBox.Text = selectedFolder;
				}
				catch (Exception)
				{
					// If something goes wrong while completing the export jobs, we will display an error message.
					string errorMessage = "Something went wrong while fetching files from input folder.";
					string errorCaption = "Input Error";
					MessageBoxButton errorButton = MessageBoxButton.OK;
					MessageBoxImage errorIcon = MessageBoxImage.Error;
					MessageBox.Show(errorMessage, errorCaption, errorButton, errorIcon);

					// Clear the file list box and give it an error message.
					FileListBox.Items.Clear();
					FileListBox.Items.Add("Unable to fetch file list.");
				}
			}
			else if (sender.Equals(OutputBrowseButton))
			{
				// Set the output box to the folder name.
				OutputTextBox.Text = selectedFolder;
				output_folder = selectedFolder;
			}
		}

		/// <summary>
		/// An event listener for when the audio channel UI element is changed.
		/// Updates the member variable corresponding to the audio channel selection from the user.
		/// </summary>
		private void Channel_RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			// Set the mono flag depending on the selection from the user.
			if (sender.Equals(MonoRadioButton))
			{
				audio_channel_selection = MONO_CHANNEL_SELECTION;
			}
			else
			{
				audio_channel_selection = STEREO_CHANNEL_SELECTION;
			}
		}

		/// <summary>
		/// An event listener for when the sample rate UI element is changed.
		/// Updates the member variable corresponding ot the sample rate selection from the user.
		/// </summary>
		private void SampleRate_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Set the sample rate selection based on the user input.
			if (SampleRate_ComboBox.SelectedValue != null)
			{
				// Gets the currently selected item as a string without the units.
				var selectedItem = SampleRate_ComboBox.SelectedValue.ToString();
				selectedItem = selectedItem!.Split(' ')[0];

				// Converts the string to an integer and converts it from kHz to Hz.
				double double_selection = Double.Parse(selectedItem);
				sample_rate_selection = (int)(double_selection * 1000);
			}
		}

		/// <summary>
		/// An event listner for when the audio track UI element is changed.
		/// Updates the member variable corresponding ot the audio track selection from the user.
		/// </summary>
		private void Track_RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			// Set the track selection based on the user input.
			if (sender.Equals(AllTracksRadioButton))
			{
				track_selection = Track.Both;
			}
			else if (sender.Equals(TrackOneRadioButton))
			{
				track_selection = Track.One;
			}
			else
			{
				track_selection = Track.Two;
			}
		}

		/// <summary>
		/// An event listener for when the export button UI element is clicked.
		/// Starts the export jobs for the files in the input directory with the user input parameters.
		/// If something is wrong with the input or output directory, a message will be shown and this method will return.
		/// </summary>
		private async void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			// Check if there are any files in the input directory.
			if (input_file_list.Count == 0)
			{
				// If there is no input files in the input directory, we will display an error message.
				string errorMessageBoxText = "There are no files in the input directory.";
				string errorCaption = "Input Error";
				MessageBoxButton errorButton = MessageBoxButton.OK;
				MessageBoxImage errorIcon = MessageBoxImage.Error;
				MessageBox.Show(errorMessageBoxText, errorCaption, errorButton, errorIcon);
				return;
			}

			// Check to make sure the output directory is still accessible.
			if (output_folder == null || !Directory.Exists(output_folder))
			{
				// If something has gone wrong with the output directory we will display an error message.
				string errorMessageBoxText = "There is a problem with the output directory.";
				string errorCaption = "Output Error";
				MessageBoxButton errorButton = MessageBoxButton.OK;
				MessageBoxImage errorIcon = MessageBoxImage.Error;
				MessageBox.Show(errorMessageBoxText, errorCaption, errorButton, errorIcon);
				return;
			}

			// Disable the export button so the user can't start multiple export jobs at the same time.
			ExportButton.IsEnabled = false;

			try
			{
				// Create a list of tasks so we can keep track of how many jobs have been completed.
				List<Task> tasks = new();

				// Start a job for each input file in the list.
				foreach (string input_file in input_file_list)
				{
					// Verify that the input file still exists.
					if (File.Exists(input_file))
					{
						// Get the file name without the extension on the end.
						var tempArr = input_file.Split('\\');
						var tempStr = tempArr[tempArr.Length - 1];
						var filename = tempStr.Substring(0, tempStr.Length - 4);

						// Get the output file name with the path included.
						var output_file = output_folder + "\\" + filename;

						// Finalize the output filenames and export the file based on what track is selected.
						if (track_selection == Track.Both || track_selection == Track.One)
						{
							// Start the task and add it to the list of tasks.
							var output_file_1 = output_file + "-1.wav";
							tasks.Add(Task.Run(() => ExportJob(input_file, output_file_1, Track.One)));
						}

						if (track_selection == Track.Both || track_selection == Track.Two)
						{
							// Start the task and add it to the list of tasks.
							var output_file_2 = output_file + "-2.wav";
							tasks.Add(Task.Run(() => ExportJob(input_file, output_file_2, Track.Two)));
						}
					}
				}

				// Wait until all the export tasks are done. then sanitize the progress UI elements.
				await Task.WhenAll(tasks);

				// Display a message box that notifies the user that the export jobs have been completed.
				string messageBoxText = "Export jobs complete.";
				string caption = "Batch Export";
				MessageBoxButton button = MessageBoxButton.OK;
				MessageBoxImage icon = MessageBoxImage.Information;
				MessageBox.Show(messageBoxText, caption, button, icon);
			}
			catch(Exception ex)
			{
				// If something goes wrong while completing the export jobs, we will display an error message.
				string errorCaption = "Export Error";
				MessageBoxButton errorButton = MessageBoxButton.OK;
				MessageBoxImage errorIcon = MessageBoxImage.Error;
				MessageBox.Show(ex.Message, errorCaption, errorButton, errorIcon);
			}
			finally
			{
				// Sanitize the progress UI elements and renable the export button.
				SanitizeProgressUIElements();
				ExportButton.IsEnabled = true;
			}
		}

		/// <summary>
		/// Executes a single export job via FFMPEG.
		/// Starts a new process and runs ffmpeg.exe with the arguments specified.
		/// This process runs in a hidden window and will automatically update progress UI elements when it finsishes.
		/// </summary>
		/// <param name="input_file">the input file to get the audio from.</param>
		/// <param name="output_file">where the new output file should be made.</param>
		/// <param name="track">the track to export from the input file.</param>
		private void ExportJob(string input_file, string output_file, Track track)
		{
			try
			{
				// Start a process that will open ffmpeg with the provided arugments.
				using (Process p = new Process())
				{
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.CreateNoWindow = true;
					p.StartInfo.RedirectStandardOutput = true;
					p.StartInfo.FileName = "ffmpeg.exe";

					// Get the audio track this job is exporting in a usable format.
					int audio_track;
					if (track == Track.One)
					{
						audio_track = 0;
					}
					else
					{
						audio_track = 1;
					}

					// Build the arugments needed for the export process.
					string arguements = $"-y -i \"{input_file}\" -map 0:a:{audio_track} -acodec pcm_s16le -ar {sample_rate_selection} -ac {audio_channel_selection} \"{output_file}\"";

					p.StartInfo.Arguments = arguements;

					// Start the process and then wait for it to finish.
					p.Start();
					p.WaitForExit();
				}
			}
			catch (Exception ex)
			{
				// If something goes wrong while completing the export job, we will display an error message.
				string errorCaption = "Export Error";
				MessageBoxButton errorButton = MessageBoxButton.OK;
				MessageBoxImage errorIcon = MessageBoxImage.Error;
				MessageBox.Show(ex.Message, errorCaption, errorButton, errorIcon);
			}
			finally
			{
				// Update the progress UI elements.
				this.Dispatcher.Invoke(() =>
				{
					// Compute the total number of jobs for the export.
					double total_jobs = input_file_list.Count;
					if (track_selection == Track.Both)
					{
						// If we are outputting both of the audio tracks, then the number of jobs will be doubled.
						// This is because we cannot export both tracks with one process. We must export each track as a separate process.
						total_jobs *= 2;
					}

					// Get the current amount of jobs completed and increment it by one.
					double completed = Double.Parse(ExportProgressCount.Text.Split("/")[0]);
					completed++;

					// Compute the percentage of jobs completed and round the number.
					double percent = Math.Round((completed / total_jobs) * 100);

					// Update the progress UI elements.
					ExportProgressCount.Text = $"{completed}/{total_jobs}";
					ExportProgressPercent.Text = $"{percent}%";
					ExportProgressBar.Value = percent;
				});
			}
		}

		/// <summary>
		/// Sanitizes the progress UI elements.
		/// All UI elements relating to the export progress will be set back to their defaults.
		/// </summary>
		private void SanitizeProgressUIElements()
		{
			// Set the UI elements to the default values.
			ExportProgressCount.Text = "0/0";
			ExportProgressPercent.Text = "0%";
			ExportProgressBar.Value = 0;
		}
	}
}
