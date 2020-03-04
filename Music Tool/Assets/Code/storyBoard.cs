using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.Threading;

using DSPLib;

public class storyBoard : EditorWindow
{
	//Tool display Data
	#region ToolInfo
	int toolBarInt = 0;
	string[] toolBarNames = {"Main", "Settings"};

	int complexityInt = 0;
	string[] complexityNames = {"Simple", "Intermediate", "Complex" };
	Rect rectSize = new Rect(40, 80, 200, 200);
	bool showCurves = false;
	#endregion

	//Actual Song data
	#region soundData
	AudioClip soundFile;
	int numSamples = 0;
	int numChannels = 0;
	float lengthOfSong = 0;
	int sampleRate = 0;

	//Simple Data
	int bpm = 0;
	AnimationCurve importantMoments;
	//Intermediate Data
	int frequency = 0; //supplied by audioClip, only an int, in Hz
	AnimationCurve freqCurve;
	int amplitude = 0;
	AnimationCurve ampCurve;
	//Complex Data
	string timeSig;
	AnimationCurve spectrumCurve;

	float[] allChannelsSamples;
	float[] amplitudes;
	float[] spectrums;
	float[] frequencies;

	#endregion

	[MenuItem("Window/Scooby Doo")]
	static void Init()
	{
		storyBoard window = (storyBoard)GetWindow(typeof(storyBoard));
		window.Show();
	}

	private void OnGUI()
	{
		toolBarInt = GUILayout.Toolbar(toolBarInt, toolBarNames);

		switch(toolBarInt)
		{
			case 0:
				{
					DrawMain();
					break;
				}
			case 1:
				{
					DrawSettings();
					break;
				}
			default:
				break;
		}
	}

	void DrawMain()
	{
		soundFile = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Audio File", "The file that will be used to generate data."), soundFile, typeof(AudioClip), false);

		if (GUILayout.Button("Analyze Real Quick"))
		{
			//Populate an array full of all the samples in the audiofile. This will be big. Like, millions big. Multithread is possible
			numSamples = soundFile.samples;
			numChannels = soundFile.channels;
			lengthOfSong = soundFile.length;

			allChannelsSamples = new float[numSamples * numChannels];
			soundFile.GetData(allChannelsSamples, 0);

			sampleRate = soundFile.frequency;

			Thread myThread = new Thread(this.AnalyzeAudio);
			myThread.Start();
		}

		switch (complexityInt)
		{
			case 0:
				{
					SimpleMenu();
					break;
				}
			case 1:
				{
					IntermediateMenu();
					break;
				}
			case 2:
				{
					ComplexMenu();
					break;
				}
			default:
				break;
		}

		if(GUILayout.Button("Create Audio Data Scriptable Object"))
		{
			BeatData asset = CreateInstance<BeatData>();
			asset.time = new float[importantMoments.length];

			asset.song = soundFile;

			for (int i = 0; i < importantMoments.length; i++)
			{
				asset.time[i] = importantMoments[i].time;
			}

			AssetDatabase.CreateAsset(asset, string.Format("Assets/BeatData/{0}.asset", soundFile.name));
			AssetDatabase.SaveAssets();

			EditorUtility.FocusProjectWindow();
		}
	}

	void SimpleMenu()
	{
		//Simple Data
		//int bpm = 0;
		bpm = EditorGUILayout.IntField(new GUIContent("BPM", "BPM of sound file"), bpm);
		if (showCurves)
		{
			importantMoments = EditorGUILayout.CurveField(new GUIContent("Interesting Points", "The points of the file where a beat of interest occurs."), importantMoments);
			//importantMoments = EditorGUI.CurveField(rectSize, new GUIContent("Spec Curve", "The spectrum data visualization."), importantMoments);
		}
	}

	void IntermediateMenu()
	{
		SimpleMenu();
		//int frequency = 0;
		//AnimationCurve freqCurve;
		//int amplitude = 0;
		//AnimationCurve ampCurve;
		frequency = soundFile.frequency;
		EditorGUILayout.IntField(new GUIContent("Frequency", "Frequency of some point??"), frequency);
		if (showCurves)
		{
			freqCurve = EditorGUILayout.CurveField(new GUIContent("Freq Curve", "The frequency visualization."), freqCurve); //, Color.blue, rectSize
		}
		amplitude = EditorGUILayout.IntField(new GUIContent("Amplitude", "Amplitude? I guess?"), amplitude);
		if (showCurves)
			ampCurve = EditorGUILayout.CurveField(new GUIContent("Amp Curve", "The amplitude visualization."), ampCurve, Color.red, rectSize);

	}

	void ComplexMenu()
	{
		//string timeSig;
		//AnimationCurve spectrumCurve;
		IntermediateMenu();

		timeSig = EditorGUILayout.TextField(new GUIContent("Time Sig", "The time signature of the piece."), timeSig);
		if (showCurves)
			spectrumCurve = EditorGUILayout.CurveField(new GUIContent("Spec Curve", "The spectrum data visualization."), spectrumCurve, Color.yellow, rectSize);
	}

	void AnalyzeAudio()
	{
		if(soundFile)
		{
			//The array for the averaged sample data. (L,R,L,R,L,R are averaged into (L+R)/2, (L+R)/2, (L+R)/2)
			float[] preprocessedSamples = new float[numSamples];

			int numberOfSamplesProcessed = 0;
			float combinedChannelAverage = 0f;

			Debug.Log("Starting sample processing...");
			for (int i = 0; i < allChannelsSamples.Length; i++)
			{
				combinedChannelAverage += allChannelsSamples[i];
				//for(int j = 0; j < numChannels; j++)
				//{
				//	combinedChannelAverage += allChannelsSamples[i + j];
				//	numberOfSamplesProcessed++;
				//}
				//preprecessedSamples[i/numChannels] = combinedChannelAverage / (float)numChannels;
				//combinedChannelAverage = 0;

				// Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
				if ((i + 1) % numChannels == 0)
				{
					preprocessedSamples[numberOfSamplesProcessed] = combinedChannelAverage / numChannels;
					numberOfSamplesProcessed++;
					combinedChannelAverage = 0f;
				}
			}

			int specSampSize = 1024;
			int iterations = preprocessedSamples.Length / specSampSize;
			double[] sampleChunk = new double[specSampSize];

			//LomFFT fft = new LomFFT();
			FFT fft = new FFT();
			fft.Initialize((System.UInt32)specSampSize);

			SpectralFluxAnalyzer preproAnalyzer = new SpectralFluxAnalyzer();

			for(int i = 0; i < iterations; ++i)
			{
				System.Array.Copy(preprocessedSamples, i * specSampSize, sampleChunk, 0, specSampSize);

				double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)specSampSize);
				double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
				double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

				// Perform the FFT and convert output (complex numbers) to Magnitude
				System.Numerics.Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
				double[] scaledFFTSpectrum = DSP.ConvertComplex.ToMagnitude(fftSpectrum);
				scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);
				
				//old
				//fft.FFT(sampleChunk);



				float currTime = getTimeFromIndex(i) * specSampSize;
				preproAnalyzer.analyzeSpectrum(System.Array.ConvertAll(scaledFFTSpectrum, x => (float)x), currTime); //AnalyzeSpectrum(data...);
			}

			//foreach(SpectralFluxAnalyzer.SpectralFluxInfo specInfo in preproAnalyzer.spectralFluxSamples)
			//{
			//	if(specInfo.isPeak)
			//	{
			//		Debug.Log("Peak at: " + specInfo.time);
			//	}
			//}

			importantMoments = null;
			importantMoments = new AnimationCurve();
			freqCurve = null;
			freqCurve = new AnimationCurve();

			Debug.Log("Starting graph processing...");
			for (int i = 0; i < preproAnalyzer.spectralFluxSamples.Count; i++)
			{
				if (preproAnalyzer.spectralFluxSamples[i].isPeak)
				{
					importantMoments.AddKey(preproAnalyzer.spectralFluxSamples[i].time, 1);
					freqCurve.AddKey(preproAnalyzer.spectralFluxSamples[i].time, preproAnalyzer.spectralFluxSamples[i].spectralFlux);
				}
			}

			Debug.Log("Done!");
			Debug.Log(numberOfSamplesProcessed);


			//AudioListener.GetSpectrumData(spectrums, 0, FFTWindow.BlackmanHarris);
			//Debug.Log(AudioSettings.outputSampleRate);
		}
	}

	void DrawSettings()
	{
		complexityInt = EditorGUILayout.Popup("Details", complexityInt, complexityNames);
		showCurves = EditorGUILayout.ToggleLeft(new GUIContent("Curves", "Toggle to display visualization of audio data."), showCurves);
	}

	///--------------------------------------------------------------------------------------------------------------------------------------------------------------
	public int getIndexFromTime(float curTime, float clipLength, float totalSampleCount)
	{
		float lengthPerSample = clipLength / totalSampleCount;

		return Mathf.FloorToInt(curTime / lengthPerSample);
	}

	public float getTimeFromIndex(int index)
	{
		return ((1f / sampleRate) * index);
	}

}

/// <summary>
/// From GiantScam BeatMapping tutorial
/// </summary>
public class SpectralFluxAnalyzer
{
	int numSamples = 1024;
	float[] curSpectrum;
	float[] prevSpectrum;

	float thresholdMult = 2.0f;
	int thresholdSize = 50;
	int indexToProcess;

	public List<SpectralFluxInfo> spectralFluxSamples;

	public SpectralFluxAnalyzer()
	{
		spectralFluxSamples = new List<SpectralFluxInfo>();

		indexToProcess = thresholdSize / 2;

		curSpectrum = new float[numSamples];
		prevSpectrum = new float[numSamples];
	}

	public class SpectralFluxInfo
	{
		public float time;
		public float spectralFlux;
		public float threshold;
		public float prunedSpectralFlux;
		public bool isPeak;
	}

	public void setCurSpectrum(float[] spectrum)
	{
		curSpectrum.CopyTo(prevSpectrum, 0);
		spectrum.CopyTo(curSpectrum, 0);
	}

	float calculateRectifiedSpectralFlux()
	{
		float sum = 0f;

		// Aggregate positive changes in spectrum data
		for (int i = 0; i < numSamples; i++)
		{
			sum += Mathf.Max(0f, curSpectrum[i] - prevSpectrum[i]);
		}
		return sum;
	}

	float getFluxThreshold(int spectralFluxIndex)
	{
		// How many samples in the past and future we include in our average
		int windowStartIndex = Mathf.Max(0, spectralFluxIndex - thresholdSize / 2);
		int windowEndIndex = Mathf.Min(spectralFluxSamples.Count - 1, spectralFluxIndex + thresholdSize / 2);

		// Add up our spectral flux over the window
		float sum = 0f;
		for (int i = windowStartIndex; i < windowEndIndex; i++)
		{
			sum += spectralFluxSamples[i].spectralFlux;
		}

		// Return the average multiplied by our sensitivity multiplier
		float avg = sum / (windowEndIndex - windowStartIndex);
		return avg * thresholdMult;
	}


	float getPrunedSpectralFlux(int spectralFluxIndex)
	{
		return Mathf.Max(0f, spectralFluxSamples[spectralFluxIndex].spectralFlux - spectralFluxSamples[spectralFluxIndex].threshold);
	}

	bool isPeak(int spectralFluxIndex)
	{
		if (spectralFluxSamples[spectralFluxIndex].prunedSpectralFlux > spectralFluxSamples[spectralFluxIndex + 1].prunedSpectralFlux &&
			spectralFluxSamples[spectralFluxIndex].prunedSpectralFlux > spectralFluxSamples[spectralFluxIndex - 1].prunedSpectralFlux)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public void analyzeSpectrum(float[] spectrum, float time)
	{
		// Set spectrum
		setCurSpectrum(spectrum);

		// Get current spectral flux from spectrum
		SpectralFluxInfo curInfo = new SpectralFluxInfo();
		curInfo.time = time;
		curInfo.spectralFlux = calculateRectifiedSpectralFlux();
		spectralFluxSamples.Add(curInfo);

		// We have enough samples to detect a peak
		if (spectralFluxSamples.Count >= thresholdSize)
		{
			// Get Flux threshold of time window surrounding index to process
			spectralFluxSamples[indexToProcess].threshold = getFluxThreshold(indexToProcess);

			// Only keep amp amount above threshold to allow peak filtering
			spectralFluxSamples[indexToProcess].prunedSpectralFlux = getPrunedSpectralFlux(indexToProcess);

			// Now that we are processed at n, n-1 has neighbors (n-2, n) to determine peak
			int indexToDetectPeak = indexToProcess - 1;

			bool curPeak = isPeak(indexToDetectPeak);

			if (curPeak)
			{
				spectralFluxSamples[indexToDetectPeak].isPeak = true;
			}
			indexToProcess++;
		}
		else
		{
			//Debug.Log(string.Format("Not ready yet.  At spectral flux sample size of {0} growing to {1}", spectralFluxSamples.Count, thresholdSize));
		}
	}
}