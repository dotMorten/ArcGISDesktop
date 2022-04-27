﻿using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Xaml.Input;

namespace ArcGISRuntimeDesktop.Controls;

public sealed partial class DocumentView : UserControl
{
    public DocumentView()
    {
        this.InitializeComponent();
    }

    public Document? Document
    {
        get { return (Document)GetValue(DocumentProperty); }
        set { SetValue(DocumentProperty, value); }
    }

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(Document), typeof(DocumentView), new PropertyMetadata(null, (s,e) => ((DocumentView)s).OnDocumentPropertyChanged(e)));

    private void OnDocumentPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        var document = e.NewValue as Document;
        if (e.NewValue is MapDocument mapDocument)
        {
            ContentArea.ContentTemplate = (DataTemplate)Resources["mapTemplate"];
        }
        else if (e.NewValue is SceneDocument sceneDocument)
        {
            ContentArea.ContentTemplate = (DataTemplate)Resources["sceneTemplate"];
        }
        else
            ContentArea.Content = null;
        if(e.OldValue is Document old)
            old.ViewpointRequested -= Document_ViewpointRequested;
        if (document != null)
            document.ViewpointRequested += Document_ViewpointRequested;
    }

    private void Document_ViewpointRequested(object? sender, Viewpoint e)
    {
        var g = ContentArea.Content as GeoView;
        _ = activeView?.SetViewpointAsync(e);
    }

    private void GeoView_PointerMoved(object? sender, PointerRoutedEventArgs e)
    {
        MapPoint? point = null;
        if (sender is MapView mv)
        {
            point = mv.ScreenToLocation(e.GetCurrentPoint(mv).Position);
        }
        else if (sender is SceneView sv)
        {
            point = sv.ScreenToBaseSurface(e.GetCurrentPoint(sv).Position);
        }
        if (point != null && !point.IsEmpty)
            PointerPosition.Text = CoordinateFormatter.ToLatitudeLongitude(point, LatitudeLongitudeFormat.DecimalDegrees, 6);
        else
            PointerPosition.Text = String.Empty;
    }
    
    private async void GeoView_Drop(object sender, DragEventArgs e)
    {
        if (Document is null)
            return;
        GeoView geoView = (GeoView)sender;
        var deferral = e.GetDeferral();
        
        Envelope? extent = null;
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            extent = await Document.Add(items);
        }
        finally
        {
            deferral.Complete();
        }
        if(extent != null)
            _ = geoView.SetViewpointAsync(new Viewpoint(extent));
    }

    private async void GeoView_DragOver(object sender, DragEventArgs e)
    {
        var dv = e.DataView;
        if (dv is null)
            return;
        var deferral = e.GetDeferral();
        try
        {
            var items = await dv.GetStorageItemsAsync();
            if(Document?.CanAdd(items) == true)
                e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private GeoView? activeView;

    private void GeoView_Loaded(object sender, RoutedEventArgs e)
    {
        activeView = sender as GeoView;
        if(activeView is MapView mapView)
        {
            mapView.LocationDisplay.DataSource = ApplicationViewModel.Instance.LocationDataSource;
            if (Document is MapDocument mapdoc)
                mapdoc.ActiveLocationDisplay = mapView.LocationDisplay;
        }
    }
}
