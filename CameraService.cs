using System;
using System.Drawing;
using AForge.Video;
using AForge.Video.DirectShow;

public class CameraService
{
    private FilterInfoCollection videoDevices;
    private VideoCaptureDevice videoSource;
    public bool IsCameraAvailable { get; private set; }

    public CameraService()
    {
        videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        if (videoDevices.Count == 0)
            IsCameraAvailable = false;
        else
        {
            IsCameraAvailable = true;
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
        }
    }

    public void TakePicture(Action<Bitmap> onPictureTaken)
    {
        if (!IsCameraAvailable)
        {
            Console.WriteLine("No camera available");
            return;
        }

        videoSource.NewFrame += (sender, eventArgs) =>
        {
            Bitmap frame = eventArgs.Frame.Clone() as Bitmap;

            onPictureTaken?.Invoke(frame);

            StopCamera();
        };

        videoSource.Start();
    }

    private void StopCamera()
    {
        if (videoSource != null && videoSource.IsRunning)
        {
            videoSource.SignalToStop();
            videoSource.WaitForStop();
        }
    }
}
