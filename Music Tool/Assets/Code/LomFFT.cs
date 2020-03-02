using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LomFFT
{
	public void FFT(float[] data)
	{
		int numSam = data.Length;

		if((numSam & (numSam - 1)) != 0)
		{
			Debug.LogError("Data is not a multiple of 2!");
			return;
		}

		numSam /= 2;

		Reverse(data, numSam);

		int mmax = 1;

		while(numSam > mmax)
		{
			int isStep = 2 * mmax;

			float theta = Mathf.PI / mmax; //int?
			float wr = 1, wi = 0;
			float wpr = Mathf.Cos(theta);
			float wpi = Mathf.Sin(theta);

			for (int m = 0; m < isStep; m += 2)
			{
				for (var k = m; k < 2 * numSam; k += 2 * isStep)
				{
					int j = k + isStep;
					float tempr = wr * data[j] - wi * data[j + 1];
					float tempi = wi * data[j] + wr * data[j + 1];
					data[j] = data[k] - tempr;
					data[j + 1] = data[k + 1] - tempi;
					data[k] = data[k] + tempr;
					data[k + 1] = data[k + 1] + tempi;
				}
				float t = wr;
				wr = wr * wpr - wi * wpi;
				wi = wi * wpr + t * wpi;
			}
			mmax = isStep;
		}
		Scale(data, numSam);
	}

	void Scale(float[] data, int numSam)
	{
		float scale = Mathf.Pow(numSam, (-1) / 2.0f);

		for(int i = 0; i < data.Length; ++i)
		{
			data[i] *= scale;
		}
	}

	void Reverse(float[] data, int numSam)
	{
		int j = 0, k = 0;
		int top = numSam / 2;

		while (true)//uuuhhhh
		{
			float t = data[j + 2];
			data[j + 2] = data[k + numSam];
			data[k + numSam] = t;
			t = data[j + 3];
			data[j + 3] = data[k + numSam + 1];
			data[k + numSam + 1] = t;

			if(j > k)
			{
				t = data[j];
				data[j] = data[k];
				data[k] = t;
				t = data[j + 1];
				data[j + 1] = data[k + 1];
				data[k + 1] = t;

				t = data[j + numSam + 2];
				data[j + numSam + 2] = data[k + numSam + 2];
				data[k + numSam + 2] = t;
				t = data[j + numSam + 3];
				data[j + numSam + 3] = data[k + numSam + 3];
				data[k + numSam + 3] = t;
			}

			k += 4;
			if(k >= numSam)
			{
				break;
			}

			int h = top;

			while(j >= h)
			{
				j -= h;
				h /= 2;
			}
			j += h;
		}
	}


}
