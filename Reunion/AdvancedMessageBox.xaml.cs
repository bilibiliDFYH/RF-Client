// <copyright file="AdvancedMessageBox.xaml.cs" company="Snowy Studio">
// Copyright (c) Snowy Studio. All rights reserved.
// </copyright>

namespace Reunion;

using System.Windows;

/// <summary>
/// Interaction logic for AdvancedMessageBox.xaml.
/// </summary>
public partial class AdvancedMessageBox : Window
{
    public AdvancedMessageBox() => InitializeComponent();

    public object? Result { get; set; }
}