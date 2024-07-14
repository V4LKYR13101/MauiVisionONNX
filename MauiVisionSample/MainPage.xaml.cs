// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Android.Renderscripts;

namespace MauiVisionSample;

//using Microsoft.Maui.Platform;
//using Microsoft.ML.OnnxRuntime;

enum ImageAcquisitionMode
{
    Sample,
    Capture,
    Pick
}

public partial class MainPage : ContentPage
{
    IVisionSample _mobilenet;
    IVisionSample _ultraface;

    IVisionSample Mobilenet => _mobilenet ??= new MobilenetSample();
    IVisionSample Ultraface => _ultraface ??= new UltrafaceSample();

    public MainPage()
    {
        InitializeComponent();
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += (s, e) =>
        {
            // Handle the tap event
            MyCamera.CaptureImage(CancellationToken.None);
        };
        MyCamera.GestureRecognizers.Add(tapGestureRecognizer);
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    async Task UpdateExecutionProviderAsync()
    {
        var executionProvider = ExecutionProviders.CPU;
        IVisionSample sample = Mobilenet;

        await sample.UpdateExecutionProviderAsync(executionProvider);
    }
    private void MyCamera_MediaCaptured(object? sender, CommunityToolkit.Maui.Views.MediaCapturedEventArgs e)
    {
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var imageData = StreamToByteArray(e.Media);
            IVisionSample sample = Mobilenet;

            try
            {
                var result = await sample.ProcessImageAsync(imageData);
                Caption.Text = result.Caption;
            }
            catch (Exception eX)
            {


            }
        });

    }

    public byte[] StreamToByteArray(Stream inputStream)
    {
        if (inputStream == null)
            throw new ArgumentNullException(nameof(inputStream));

        using (MemoryStream memoryStream = new MemoryStream())
        {
            inputStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }

    Task<byte[]> GetSampleImageAsync() => Task.Run(() =>
    {
        var assembly = GetType().Assembly;

        var imageName = "";

        if (string.IsNullOrWhiteSpace(imageName))
        {
            return null;
        }

        return Utils.LoadResource(imageName).Result;
    });


    async Task<byte[]> GetBytesFromPhotoFile(FileResult fileResult)
    {
        byte[] bytes;

        using Stream stream = await fileResult.OpenReadAsync();
        using MemoryStream ms = new MemoryStream();

        stream.CopyTo(ms);
        bytes = ms.ToArray();

        return bytes;
    }

    void ClearResult() => MainThread.BeginInvokeOnMainThread(() =>
    {
        Caption.Text = string.Empty;
    });

    void ShowResult(byte[] image, string caption = null) => MainThread.BeginInvokeOnMainThread(() =>
    {
        Caption.Text = string.IsNullOrWhiteSpace(caption) ? string.Empty : caption;
    });

    ImageAcquisitionMode GetAcquisitionModeFromText(string tag) => tag switch
    {
        nameof(ImageAcquisitionMode.Capture) => ImageAcquisitionMode.Capture,
        nameof(ImageAcquisitionMode.Pick) => ImageAcquisitionMode.Pick,
        _ => ImageAcquisitionMode.Sample
    };

    async void AcquireButton_Clicked(object sender, EventArgs e)
    {
        var i = 0;
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await MyCamera.CaptureImage(CancellationToken.None);
        });
    }

    private void ExecutionProviderOptions_SelectedIndexChanged(object sender, EventArgs e)
        => UpdateExecutionProviderAsync().ContinueWith((task)
            =>
        {
            if (task.IsFaulted) MainThread.BeginInvokeOnMainThread(()
            => DisplayAlert("Error", task.Exception.Message, "OK"));
        });

    private void Models_SelectedIndexChanged(object sender, EventArgs e)
        // make sure EP is in sync
        => ExecutionProviderOptions_SelectedIndexChanged(null, null);
}


