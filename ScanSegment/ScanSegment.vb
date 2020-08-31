Imports OfficeOpenXml
Imports Emgu.CV
Imports System.Collections.Concurrent
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization
Imports System.Windows
Imports System.Windows.Forms
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Module ScanSegment
    Private Const Resolution096 As Single = 96
    Private Const SaveDirectory As String = "\ScanSegment\"
    Private Const SuperpixelDirectory As String = "Segmented\"
    Private Const GroundTruthDirectory As String = "\GroundTruth\"
    Private Const GroundTruthBoundariesDirectory As String = "\GroundTruthBoundaries\"
    Private Const GroundTruthSegmentationDirectory As String = "\GroundTruthSegmentation\"
    Private Const OutlineImagesDirectory As String = "\OutlineImages\"
    Private Const SegmentationImagesDirectory As String = "\SegmentationImages\"
    Private Const NoiseOutlineImagesDirectory As String = "\NoiseOutlineImages\"
    Private Const NoiseSegmentationImagesDirectory As String = "\NoiseSegmentationImages\"
    Private Const UnsplashImagesDirectory As String = "\Unsplash\"
    Private Const MulMin As Single = 0.1
    Private Const MulMax As Single = 5.0
    Private Const MulStepBig As Single = 0.1
    Private Const MulStepSmall As Single = 0.01
    Private Const MulFactor As Integer = 1000
    Private Const CPUUtilisationMedium As Integer = 80
    Private Const CPUTolerance As Integer = 10
    Private Const DataStr As String = "Data"
    Private Const ComplexityStr As String = "Complexity"
    Private Const AltComplexityStr As String = "AltComplexity"
    Private Const NoiseStr As String = "Noise"
    Private Const NumStr As String = "No"
    Private Const TypeStr As String = "Type"
    Private Const SuperpixelsStr As String = "Superpixels"
    Private Const SegmentsStr As String = "Segments"
    Private Const TimeStr As String = "Time"
    Private Const BPStr As String = "BP"
    Private Const UEStr As String = "UE"
    Private Const ASAStr As String = "ASA"
    Private Const ComplexityCount As Integer = 10
    Private Const ComplexityMul As Integer = 100
    Private Const ComplexitySuperpixels As Integer = 200
    Private Const NoiseMul As Integer = 1000
    Private Const AltWidth As Integer = 3848
    Private Const AltHeight As Integer = 2568
    Private SuperpixelList As New List(Of Integer)({100, 200, 300, 400, 500, 600})
    Private m_LUT As New List(Of Matrix(Of Byte))({Nothing, Nothing, Nothing, Nothing})
    Private m_SingleProcess As List(Of SegmentType) = {SegmentType.ScanSegment}.ToList
    Private m_CombineProcess As List(Of SegmentType) = {SegmentType.DSFH, SegmentType.DSERS, SegmentType.DSCRS}.ToList ' process simultaneously (slow processes)
    Private ComplexityList As New List(Of Single)({0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0})
    Private NoiseList As New List(Of Single)({0.0, 0.025, 0.05, 0.075, 0.1, 0.125, 0.15, 0.175, 0.2})

    Sub Main()
        Dim oFolderBrowserDialog As New FolderBrowserDialog
        oFolderBrowserDialog.Description = "ScanSegment Test Images"
        oFolderBrowserDialog.ShowNewFolderButton = False
        oFolderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop
        If oFolderBrowserDialog.ShowDialog = DialogResult.OK Then
            Dim oDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath)
            Dim oImageFiles As New List(Of IO.FileInfo)
            oImageFiles.AddRange(oDirectoryInfo.EnumerateFiles("*.jpg", IO.SearchOption.TopDirectoryOnly))

            Dim oUnsplashDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + UnsplashImagesDirectory)
            Dim oUnsplashImageFiles As New List(Of IO.FileInfo)
            oUnsplashImageFiles.AddRange(oUnsplashDirectoryInfo.EnumerateFiles("*.jpg", IO.SearchOption.TopDirectoryOnly))

            Dim oStartDate As Date = Date.Now
            Dim iCurrentProcessing As Integer = 0
            Dim iProcessingSteps As Integer = 0

            If oImageFiles.Count > 0 Then
#Region "Directories"
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + SaveDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + SaveDirectory)
                End If
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + SaveDirectory + SuperpixelDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + SaveDirectory + SuperpixelDirectory)
                End If
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + GroundTruthBoundariesDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + GroundTruthBoundariesDirectory)
                End If
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + GroundTruthSegmentationDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + GroundTruthSegmentationDirectory)
                End If
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + OutlineImagesDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + OutlineImagesDirectory)
                End If
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + SegmentationImagesDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + SegmentationImagesDirectory)
                End If
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + NoiseOutlineImagesDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + NoiseOutlineImagesDirectory)
                End If
                If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + NoiseSegmentationImagesDirectory) Then
                    IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + NoiseSegmentationImagesDirectory)
                End If
                For Each oType In [Enum].GetValues(GetType(SegmentType))
                    Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)
                    If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + SaveDirectory + SuperpixelDirectory + sTypeName) Then
                        IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + SaveDirectory + SuperpixelDirectory + sTypeName)
                    End If
                    If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + OutlineImagesDirectory + sTypeName) Then
                        IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + OutlineImagesDirectory + sTypeName)
                    End If
                    If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + SegmentationImagesDirectory + sTypeName) Then
                        IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + SegmentationImagesDirectory + sTypeName)
                    End If
                    If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + NoiseOutlineImagesDirectory + sTypeName) Then
                        IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + NoiseOutlineImagesDirectory + sTypeName)
                    End If
                    If Not IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + NoiseSegmentationImagesDirectory + sTypeName) Then
                        IO.Directory.CreateDirectory(oFolderBrowserDialog.SelectedPath + NoiseSegmentationImagesDirectory + sTypeName)
                    End If
                Next
#End Region

                ' get multipliers for each routine to get correct superpixel sizes
#Region "Multipliers"
                Console.WriteLine(GetElapsed(oStartDate) + " Multipliers Start")

                Dim oLargeMul As New List(Of Integer)
                For i = CInt(MulMin * MulFactor) To CInt(MulMax * MulFactor) Step (MulStepBig * MulFactor)
                    oLargeMul.Add(i)
                Next

                ' run combined tasks
                If m_CombineProcess.Count > 0 Then
                    Dim oCombineActionList As New List(Of Tuple(Of Action(Of Object), Object))
                    Dim oCombineAction As Action(Of Object) = Sub(oParam As Tuple(Of List(Of IO.FileInfo), List(Of Integer), SegmentType, Date, String))
                                                                  ProcessCombinedMultiplier(oParam.Item1, oParam.Item2, oParam.Item3, oParam.Item4, oParam.Item5)
                                                              End Sub

                    For Each oType In m_CombineProcess
                        oCombineActionList.Add(New Tuple(Of Action(Of Object), Object)(oCombineAction, New Tuple(Of List(Of IO.FileInfo), List(Of Integer), SegmentType, Date, String) _
                                                                                           (oImageFiles, oLargeMul, oType, oStartDate, oFolderBrowserDialog.SelectedPath)))
                    Next

                    initScan()
                    ProtectedRunTasks(oCombineActionList)
                    exitScan()
                End If

                ' run sequential tasks
                For Each oType In [Enum].GetValues(GetType(SegmentType))
                    initScan()
                    ProcessCombinedMultiplier(oImageFiles, oLargeMul, oType, oStartDate, oFolderBrowserDialog.SelectedPath)
                    exitScan()
                Next

                ' loading multiplers
                Dim oMultiplier As New Result
                For Each oImageFile In oImageFiles
                    If Not oMultiplier.Results.ContainsKey(oImageFile.Name) Then
                        oMultiplier.Results.Add(oImageFile.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                    End If
                    For Each iSuperpixel In SuperpixelList
                        If Not oMultiplier.Results(oImageFile.Name).ContainsKey(iSuperpixel) Then
                            oMultiplier.Results(oImageFile.Name).Add(iSuperpixel, New Dictionary(Of SegmentType, Integer))
                        End If
                        For Each oType In [Enum].GetValues(GetType(SegmentType))
                            If Not oMultiplier.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType) Then
                                oMultiplier.Results(oImageFile.Name)(iSuperpixel).Add(oType, 0)
                            End If
                        Next
                    Next
                Next

                iProcessingSteps = oImageFiles.Count * SuperpixelList.Count * [Enum].GetValues(GetType(SegmentType)).GetLength(0)
                iCurrentProcessing = 0

                initScan()
                For Each oType In [Enum].GetValues(GetType(SegmentType))
                    Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)
                    Dim sTypeMultiplierFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Multipliers_" + sTypeName + ".xml"
                    If IO.File.Exists(sTypeMultiplierFile) Then
                        Dim oTypeMultiplier As Result = DeserializeDataContractFile(Of Result)(sTypeMultiplierFile, Result.GetKnownTypes, , , False)
                        Dim bTypeFileChanged As Boolean = False
                        For Each oKeyValue1 In oMultiplier.Results
                            If Not oTypeMultiplier.Results.ContainsKey(oKeyValue1.Key) Then
                                oTypeMultiplier.Results.Add(oKeyValue1.Key, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                                bTypeFileChanged = True
                            End If
                            For Each oKeyValue2 In oKeyValue1.Value
                                If Not oTypeMultiplier.Results(oKeyValue1.Key).ContainsKey(oKeyValue2.Key) Then
                                    oTypeMultiplier.Results(oKeyValue1.Key).Add(oKeyValue2.Key, New Dictionary(Of SegmentType, Integer))
                                    bTypeFileChanged = True
                                End If
                                For Each oKeyValue3 In oKeyValue2.Value
                                    If oKeyValue3.Key = oType Then
                                        If (Not oTypeMultiplier.Results(oKeyValue1.Key)(oKeyValue2.Key).ContainsKey(oKeyValue3.Key)) OrElse oTypeMultiplier.Results(oKeyValue1.Key)(oKeyValue2.Key)(oKeyValue3.Key) = 0 Then
                                            Dim oFileName As String = oFolderBrowserDialog.SelectedPath + "\" + oKeyValue1.Key
                                            Using oBitmap As New System.Drawing.Bitmap(oFileName)
                                                Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                                    Dim oBounds As New System.Drawing.Rectangle(0, 0, oMatrix.Width, oMatrix.Height)
                                                    Dim iSegments As Integer = 0
                                                    Dim iMul As Integer = GetMultiplier(oLargeMul, oBounds, oMatrix, oKeyValue2.Key, oType, iSegments)
                                                    If oTypeMultiplier.Results(oKeyValue1.Key)(oKeyValue2.Key).ContainsKey(oType) Then
                                                        oTypeMultiplier.Results(oKeyValue1.Key)(oKeyValue2.Key)(oType) = iMul
                                                    Else
                                                        oTypeMultiplier.Results(oKeyValue1.Key)(oKeyValue2.Key).Add(oType, iMul)
                                                    End If

                                                    Console.WriteLine(GetElapsed(oStartDate) + " Multiplier " + oKeyValue1.Key + ", " + oKeyValue2.Key.ToString + ", " + sTypeName + " Updated")
                                                End Using
                                            End Using
                                            bTypeFileChanged = True
                                        End If
                                    End If
                                Next
                            Next
                        Next

                        ' save file again if changed
                        If bTypeFileChanged Then
                            IO.File.Delete(sTypeMultiplierFile)
                            SerializeDataContractFile(sTypeMultiplierFile, oTypeMultiplier, Result.GetKnownTypes, , , False)
                            Console.WriteLine(GetElapsed(oStartDate) + " Multiplier " + sTypeName + " Updated")
                        End If
                    End If
                Next
                exitScan()
#End Region

                ' check that all multiplers have been initialised
                Dim bInitialised As Boolean = True
#Region "Check Multipliers"
                For Each oType In [Enum].GetValues(GetType(SegmentType))
                    Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)
                    Dim sTypeMultiplierFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Multipliers_" + sTypeName + ".xml"
                    If IO.File.Exists(sTypeMultiplierFile) Then
                        Dim oTypeMultiplier As Result = DeserializeDataContractFile(Of Result)(sTypeMultiplierFile, Result.GetKnownTypes, , , False)
                        For Each oImageFile In oImageFiles
                            If bInitialised AndAlso oTypeMultiplier.Results.ContainsKey(oImageFile.Name) Then
                                For Each iSuperpixel In SuperpixelList
                                    If bInitialised AndAlso oTypeMultiplier.Results(oImageFile.Name).ContainsKey(iSuperpixel) Then
                                        If bInitialised AndAlso oTypeMultiplier.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType) Then
                                            Dim iMul As Integer = oTypeMultiplier.Results(oImageFile.Name)(iSuperpixel)(oType)
                                            If bInitialised AndAlso iMul > 0 Then
                                                oMultiplier.Results(oImageFile.Name)(iSuperpixel)(oType) = iMul
                                            Else
                                                bInitialised = False
                                                Exit For
                                            End If
                                        Else
                                            bInitialised = False
                                            Exit For
                                        End If
                                    Else
                                        bInitialised = False
                                        Exit For
                                    End If
                                Next
                            Else
                                bInitialised = False
                                Exit For
                            End If
                        Next
                    End If
                Next
#End Region

                If bInitialised Then
                    ' dry run through all routines to start up and initialise
                    initScan()
#Region "Warmup"
                    Console.WriteLine(GetElapsed(oStartDate) + " Pretest Warmup")
                    Dim oImageFileW As IO.FileInfo = oImageFiles.First
                    Using oBitmap As New System.Drawing.Bitmap(oImageFileW.FullName)
                        Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                            Dim oBounds As New System.Drawing.Rectangle(0, 0, oMatrix.Width, oMatrix.Height)
                            Dim oLabels As Matrix(Of Integer) = Nothing
                            Dim iSegments As Integer = 0

                            For Each iSuperpixel In SuperpixelList
                                For Each oType In [Enum].GetValues(GetType(SegmentType))
                                    Segment(oBounds, oMatrix, oLabels, iSuperpixel, 1.0, True, oType, iSegments)
                                    If MatrixNotNothing(oLabels) Then
                                        oLabels.Dispose()
                                        oLabels = Nothing
                                    End If
                                Next
                            Next
                        End Using
                    End Using
#End Region

                    ' timings for all processes
#Region "Timings"
                    Console.WriteLine(GetElapsed(oStartDate) + " Timings Start")

                    iCurrentProcessing = 0
                    For Each oType In [Enum].GetValues(GetType(SegmentType))
                        Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                        Dim sTimingsFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Timings_" + sTypeName + ".xml"
                        Dim oTimings As Result = Nothing

                        If IO.File.Exists(sTimingsFile) Then
                            oTimings = DeserializeDataContractFile(Of Result)(sTimingsFile, Result.GetKnownTypes, , , False)
                        Else
                            oTimings = New Result
                        End If

                        Dim sSegmentsFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Segments_" + sTypeName + ".xml"
                        Dim oSegments As Result = Nothing

                        If IO.File.Exists(sSegmentsFile) Then
                            oSegments = DeserializeDataContractFile(Of Result)(sSegmentsFile, Result.GetKnownTypes, , , False)
                        Else
                            oSegments = New Result
                        End If

                        For i = 0 To oImageFiles.Count - 1
                            Dim oImageFile As IO.FileInfo = oImageFiles(i)

                            If oMultiplier.Results.ContainsKey(oImageFile.Name) Then
                                Using oBitmap As New System.Drawing.Bitmap(oImageFile.FullName)
                                    Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                        Dim oBounds As New System.Drawing.Rectangle(0, 0, oMatrix.Width, oMatrix.Height)
                                        Dim oLabels As Matrix(Of Integer) = Nothing
                                        Dim iSegments As Integer = 0

                                        If Not oTimings.Results.ContainsKey(oImageFile.Name) Then
                                            oTimings.Results.Add(oImageFile.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                                        End If
                                        If Not oSegments.Results.ContainsKey(oImageFile.Name) Then
                                            oSegments.Results.Add(oImageFile.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                                        End If
                                        For Each iSuperpixel In SuperpixelList
                                            If oMultiplier.Results(oImageFile.Name).ContainsKey(iSuperpixel) Then
                                                If Not oTimings.Results(oImageFile.Name).ContainsKey(iSuperpixel) Then
                                                    oTimings.Results(oImageFile.Name).Add(iSuperpixel, New Dictionary(Of SegmentType, Integer))
                                                End If
                                                If Not oSegments.Results(oImageFile.Name).ContainsKey(iSuperpixel) Then
                                                    oSegments.Results(oImageFile.Name).Add(iSuperpixel, New Dictionary(Of SegmentType, Integer))
                                                End If
                                                If ((Not oTimings.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType)) OrElse oTimings.Results(oImageFile.Name)(iSuperpixel)(oType) = 0 OrElse (Not oSegments.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType)) OrElse oSegments.Results(oImageFile.Name)(iSuperpixel)(oType) = 0) AndAlso oMultiplier.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType) Then
                                                    Dim iMul As Integer = oMultiplier.Results(oImageFile.Name)(iSuperpixel)(oType)
                                                    Dim fCurrentMul As Single = CSng(iMul) / CSng(MulFactor)

                                                    Dim iDuration As Integer = Segment(oBounds, oMatrix, oLabels, iSuperpixel, fCurrentMul, True, oType, iSegments)
                                                    If oTimings.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType) Then
                                                        oTimings.Results(oImageFile.Name)(iSuperpixel)(oType) = iDuration
                                                    Else
                                                        oTimings.Results(oImageFile.Name)(iSuperpixel).Add(oType, iDuration)
                                                    End If
                                                    If oSegments.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType) Then
                                                        oSegments.Results(oImageFile.Name)(iSuperpixel)(oType) = iSegments
                                                    Else
                                                        oSegments.Results(oImageFile.Name)(iSuperpixel).Add(oType, iSegments)
                                                    End If

                                                    If MatrixNotNothing(oLabels) Then
                                                        Dim sMeanImageFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + SuperpixelDirectory + sTypeName + "\SegmentMean_" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "_" + sTypeName + "_" + iSuperpixel.ToString + "_" + iSegments.ToString + "_[" + iDuration.ToString + "].tif"
                                                        If Not IO.File.Exists(sMeanImageFile) Then
                                                            Using oLabelByte As Matrix(Of Byte) = ConvertLabels(oLabels, oMatrix, LabelType.Mean)
                                                                SaveMatrix(sMeanImageFile, oLabelByte)
                                                            End Using
                                                        End If

                                                        Dim sOutlineImageFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + SuperpixelDirectory + sTypeName + "\SegmentOutline_" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "_" + sTypeName + "_" + iSuperpixel.ToString + "_" + iSegments.ToString + "_[" + iDuration.ToString + "].tif"
                                                        If Not IO.File.Exists(sOutlineImageFile) Then
                                                            Using oLabelByte As Matrix(Of Byte) = ConvertLabels(oLabels, oMatrix, LabelType.Outline)
                                                                SaveMatrix(sOutlineImageFile, oLabelByte)
                                                            End Using
                                                        End If

                                                        Dim sOutlineOnlyImageFile As String = oFolderBrowserDialog.SelectedPath + OutlineImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iSuperpixel.ToString + "].tif"
                                                        If Not IO.File.Exists(sOutlineOnlyImageFile) Then
                                                            Using oLabelByte As Matrix(Of Byte) = ConvertLabels(oLabels, oMatrix, LabelType.OutlineOnly)
                                                                SaveMatrix(sOutlineOnlyImageFile, oLabelByte)
                                                            End Using
                                                        End If

                                                        Dim sSegmentationImageFile As String = oFolderBrowserDialog.SelectedPath + SegmentationImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iSuperpixel.ToString + "].tif"
                                                        If Not IO.File.Exists(sSegmentationImageFile) Then
                                                            Using oUShortLabels As Matrix(Of UShort) = oLabels.Convert(Of UShort)
                                                                Using oLabelByte As Matrix(Of Byte) = ConvertUShortLabels(oUShortLabels)
                                                                    SaveMatrix(sSegmentationImageFile, oLabelByte)
                                                                End Using
                                                            End Using
                                                        End If

                                                        oLabels.Dispose()
                                                        oLabels = Nothing
                                                    End If
                                                End If

                                                iCurrentProcessing += 1
                                                Console.WriteLine(GetElapsed(oStartDate) + " Processing " + iCurrentProcessing.ToString + "/" + iProcessingSteps.ToString)
                                            End If
                                        Next
                                    End Using
                                End Using
                            End If
                        Next

                        SerializeDataContractFile(sTimingsFile, oTimings, Result.GetKnownTypes, , , False)
                        SerializeDataContractFile(sSegmentsFile, oSegments, Result.GetKnownTypes, , , False)

                        Console.WriteLine(GetElapsed(oStartDate) + " Timings " + sTypeName + " Saved")
                    Next
#End Region

                    ' computational complexity
#Region "Complexity"
                    Console.WriteLine(GetElapsed(oStartDate) + " Complexity Start")

                    iCurrentProcessing = 0
                    iProcessingSteps = Math.Min(oImageFiles.Count, ComplexityCount) * ComplexityList.Count * [Enum].GetValues(GetType(SegmentType)).GetLength(0)
                    For Each oType In [Enum].GetValues(GetType(SegmentType))
                        If oType <> SegmentType.DSFH Then
                            Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                            Dim sComplexityFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Complexity_" + sTypeName + ".xml"
                            Dim oComplexity As Result = Nothing

                            If IO.File.Exists(sComplexityFile) Then
                                oComplexity = DeserializeDataContractFile(Of Result)(sComplexityFile, Result.GetKnownTypes, , , False)
                            Else
                                oComplexity = New Result
                            End If

                            For i = 0 To Math.Min(oImageFiles.Count, ComplexityCount) - 1
                                Dim oImageFile As IO.FileInfo = oImageFiles(i)

                                Using oBitmap As New System.Drawing.Bitmap(oImageFile.FullName)
                                    Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                        If Not oComplexity.Results.ContainsKey(oImageFile.Name) Then
                                            oComplexity.Results.Add(oImageFile.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                                        End If
                                        For Each fComplexity In ComplexityList
                                            Dim iScaledWidth As Integer = CInt(CSng(oMatrix.Width) * fComplexity)
                                            Dim iScaledHeight As Integer = CInt(CSng(oMatrix.Height) * fComplexity)
                                            Dim oScaledBounds As New System.Drawing.Rectangle(0, 0, iScaledWidth, iScaledHeight)

                                            Using oScaledMatrix As New Matrix(Of Byte)(iScaledHeight, iScaledWidth, oMatrix.NumberOfChannels)
                                                CvInvoke.Resize(oMatrix, oScaledMatrix, oScaledMatrix.Size, 0, 0, CvEnum.Inter.Lanczos4)

                                                Dim iComplexity As Integer = CInt(fComplexity * CSng(ComplexityMul))
                                                If Not oComplexity.Results(oImageFile.Name).ContainsKey(iComplexity) Then
                                                    oComplexity.Results(oImageFile.Name).Add(iComplexity, New Dictionary(Of SegmentType, Integer))
                                                End If
                                                If (Not oComplexity.Results(oImageFile.Name)(iComplexity).ContainsKey(oType)) OrElse oComplexity.Results(oImageFile.Name)(iComplexity)(oType) = 0 Then
                                                    Dim oLabels As Matrix(Of Integer) = Nothing
                                                    Dim iSegments As Integer = 0

                                                    Dim iDuration As Integer = Segment(oScaledBounds, oScaledMatrix, oLabels, ComplexitySuperpixels, 1.0, True, oType, iSegments)
                                                    If oComplexity.Results(oImageFile.Name)(iComplexity).ContainsKey(oType) Then
                                                        oComplexity.Results(oImageFile.Name)(iComplexity)(oType) = iDuration
                                                    Else
                                                        oComplexity.Results(oImageFile.Name)(iComplexity).Add(oType, iDuration)
                                                    End If

                                                    If MatrixNotNothing(oLabels) Then
                                                        oLabels.Dispose()
                                                        oLabels = Nothing
                                                    End If
                                                End If
                                            End Using

                                            iCurrentProcessing += 1
                                            Console.WriteLine(GetElapsed(oStartDate) + " Processing " + iCurrentProcessing.ToString + "/" + iProcessingSteps.ToString)
                                        Next
                                    End Using
                                End Using
                            Next

                            SerializeDataContractFile(sComplexityFile, oComplexity, Result.GetKnownTypes, , , False)

                            Console.WriteLine(GetElapsed(oStartDate) + " Complexity " + sTypeName + " Saved")
                        End If
                    Next
#End Region

                    ' alternate computational complexity
#Region "AltComplexity"
                    Console.WriteLine(GetElapsed(oStartDate) + " Alternate Complexity Start")

                    iCurrentProcessing = 0
                    iProcessingSteps = oUnsplashImageFiles.Count * ComplexityList.Count * [Enum].GetValues(GetType(SegmentType)).GetLength(0)
                    For Each oType In [Enum].GetValues(GetType(SegmentType))
                        If oType <> SegmentType.DSFH Then
                            Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                            Dim sComplexityFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "AltComplexity_" + sTypeName + ".xml"
                            Dim oComplexity As Result = Nothing

                            If IO.File.Exists(sComplexityFile) Then
                                oComplexity = DeserializeDataContractFile(Of Result)(sComplexityFile, Result.GetKnownTypes, , , False)
                            Else
                                oComplexity = New Result
                            End If

                            For i = 0 To oUnsplashImageFiles.Count - 1
                                Dim oImageFile As IO.FileInfo = oUnsplashImageFiles(i)

                                Using oBitmap As New System.Drawing.Bitmap(oImageFile.FullName)
                                    Using oAltMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                        Using oMatrix As New Matrix(Of Byte)(AltHeight, AltWidth, 3)
                                            CvInvoke.Resize(oAltMatrix, oMatrix, oMatrix.Size, 0, 0, CvEnum.Inter.Lanczos4)

                                            If Not oComplexity.Results.ContainsKey(oImageFile.Name) Then
                                                oComplexity.Results.Add(oImageFile.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                                            End If
                                            For Each fAltComplexity In ComplexityList
                                                Dim fComplexity As Single = fAltComplexity / 8.0
                                                Dim iScaledWidth As Integer = CInt(CSng(oMatrix.Width) * fComplexity)
                                                Dim iScaledHeight As Integer = CInt(CSng(oMatrix.Height) * fComplexity)
                                                Dim oScaledBounds As New System.Drawing.Rectangle(0, 0, iScaledWidth, iScaledHeight)

                                                iCurrentProcessing += 1

                                                Using oScaledMatrix As New Matrix(Of Byte)(iScaledHeight, iScaledWidth, oMatrix.NumberOfChannels)
                                                    CvInvoke.Resize(oMatrix, oScaledMatrix, oScaledMatrix.Size, 0, 0, CvEnum.Inter.Lanczos4)

                                                    Dim iComplexity As Integer = CInt(fComplexity * CSng(ComplexityMul))
                                                    If Not oComplexity.Results(oImageFile.Name).ContainsKey(iComplexity) Then
                                                        oComplexity.Results(oImageFile.Name).Add(iComplexity, New Dictionary(Of SegmentType, Integer))
                                                    End If
                                                    If (Not oComplexity.Results(oImageFile.Name)(iComplexity).ContainsKey(oType)) OrElse oComplexity.Results(oImageFile.Name)(iComplexity)(oType) = 0 Then
                                                        Dim oLabels As Matrix(Of Integer) = Nothing
                                                        Dim iSegments As Integer = 0

                                                        If Not oComplexity.Results(oImageFile.Name)(iComplexity).ContainsKey(oType) Then
                                                            Dim iDuration As Integer = Segment(oScaledBounds, oScaledMatrix, oLabels, ComplexitySuperpixels, 1.0, True, oType, iSegments)
                                                            oComplexity.Results(oImageFile.Name)(iComplexity).Add(oType, iDuration)
                                                        End If

                                                        If MatrixNotNothing(oLabels) Then
                                                            oLabels.Dispose()
                                                            oLabels = Nothing
                                                        End If
                                                    End If
                                                End Using

                                                Console.WriteLine(GetElapsed(oStartDate) + " Processing " + iCurrentProcessing.ToString + "/" + iProcessingSteps.ToString)
                                            Next
                                        End Using
                                    End Using
                                End Using
                            Next

                            SerializeDataContractFile(sComplexityFile, oComplexity, Result.GetKnownTypes, , , False)

                            Console.WriteLine(GetElapsed(oStartDate) + " Alternate Complexity " + sTypeName + " Saved")
                        End If
                    Next
#End Region

                    ' noise resistance
#Region "Noise"
                    Console.WriteLine(GetElapsed(oStartDate) + " Noise Start")

                    iCurrentProcessing = 0
                    iProcessingSteps = oImageFiles.Count * NoiseList.Count * [Enum].GetValues(GetType(SegmentType)).GetLength(0)
                    For Each oType In [Enum].GetValues(GetType(SegmentType))
                        Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                        For i = 0 To oImageFiles.Count - 1
                            Dim oImageFile As IO.FileInfo = oImageFiles(i)

                            Using oBitmap As New System.Drawing.Bitmap(oImageFile.FullName)
                                Dim oLabels As Matrix(Of Integer) = Nothing
                                Dim iSegments As Integer = 0

                                For Each fNoise In NoiseList
                                    Dim iNoise As Integer = CInt(fNoise * CSng(NoiseMul))
                                    Dim sOutlineOnlyImageFile As String = oFolderBrowserDialog.SelectedPath + NoiseOutlineImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iNoise.ToString + "].tif"
                                    Dim sSegmentationImageFile As String = oFolderBrowserDialog.SelectedPath + NoiseSegmentationImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iNoise.ToString + "].tif"
                                    If (Not IO.File.Exists(sOutlineOnlyImageFile)) OrElse (Not IO.File.Exists(sSegmentationImageFile)) Then
                                        ' apply salt and pepper noise
                                        Using oNoiseBitmap As System.Drawing.Bitmap = oBitmap.Clone
                                            Dim oSaltAndPepperNoise As New Accord.Imaging.Filters.SaltAndPepperNoise(fNoise * 100)
                                            If iNoise > 0 Then
                                                oSaltAndPepperNoise.ApplyInPlace(oNoiseBitmap)
                                            End If
                                            Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oNoiseBitmap)
                                                Dim oBounds As New System.Drawing.Rectangle(0, 0, oMatrix.Width, oMatrix.Height)
                                                Segment(oBounds, oMatrix, oLabels, ComplexitySuperpixels, 1.0, True, oType, iSegments)

                                                If MatrixNotNothing(oLabels) Then
                                                    If IO.File.Exists(sOutlineOnlyImageFile) Then
                                                        IO.File.Delete(sOutlineOnlyImageFile)
                                                    End If
                                                    Using oLabelByte As Matrix(Of Byte) = ConvertLabels(oLabels, oMatrix, LabelType.OutlineOnly)
                                                        SaveMatrix(sOutlineOnlyImageFile, oLabelByte)
                                                    End Using

                                                    If IO.File.Exists(sSegmentationImageFile) Then
                                                        IO.File.Delete(sSegmentationImageFile)
                                                    End If
                                                    Using oUShortLabels As Matrix(Of UShort) = oLabels.Convert(Of UShort)
                                                        Using oLabelByte As Matrix(Of Byte) = ConvertUShortLabels(oUShortLabels)
                                                            SaveMatrix(sSegmentationImageFile, oLabelByte)
                                                        End Using
                                                    End Using

                                                    oLabels.Dispose()
                                                    oLabels = Nothing
                                                End If
                                            End Using
                                        End Using
                                    End If

                                    iCurrentProcessing += 1
                                    Console.WriteLine(GetElapsed(oStartDate) + " Processing " + iCurrentProcessing.ToString + "/" + iProcessingSteps.ToString)
                                Next
                            End Using
                        Next
                    Next
#End Region
                    exitScan()

                    ' extract ground truth from mat files
#Region "Ground Truth"
                    If IO.Directory.Exists(oFolderBrowserDialog.SelectedPath + GroundTruthDirectory) Then
                        Dim oGroundTruthDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + GroundTruthDirectory)
                        Dim oGroundTruthFiles As New List(Of IO.FileInfo)
                        oGroundTruthFiles.AddRange(oGroundTruthDirectoryInfo.EnumerateFiles("*.mat", IO.SearchOption.AllDirectories))

                        If oGroundTruthFiles.Count > 0 Then
                            Dim iProcessingStepsGT As Integer = oGroundTruthFiles.Count
                            Dim iCurrentProcessingGT As Integer = 0

                            For Each oGroundTruthFile In oGroundTruthFiles
                                Dim sFileString As String = Left(oGroundTruthFile.Name, Len(oGroundTruthFile.Name) - Len(oGroundTruthFile.Extension))
                                Using oMatReader As New Accord.IO.MatReader(oGroundTruthFile.FullName)
                                    Dim oFieldNames As List(Of String) = oMatReader.FieldNames.ToList
                                    If oFieldNames.Count = 1 Then
                                        Dim oNode As Accord.IO.MatNode = oMatReader.Fields.First.Value
                                        For i = 0 To oNode.Fields.Count - 1
                                            Dim oNode2 As Accord.IO.MatNode = oNode.Fields(i)
                                            If oNode2.Fields.Count = 2 Then
                                                Dim sGroundTruthBoundariesName As String = oFolderBrowserDialog.SelectedPath + GroundTruthBoundariesDirectory + sFileString + "[" + i.ToString + "].tif"
                                                If Not IO.File.Exists(sGroundTruthBoundariesName) Then
                                                    Dim oNodeBoundaries As Accord.IO.MatNode = oNode2.Fields("Boundaries")
                                                    If oNodeBoundaries.Value.GetType.Equals(GetType(Byte(,))) Then
                                                        Dim oBoundaries As Byte(,) = oNodeBoundaries.Value
                                                        Dim iHeight As Integer = oBoundaries.GetLength(0)
                                                        Dim iWidth As Integer = oBoundaries.GetLength(1)

                                                        ' create ground truth matrix if not present
                                                        Using oGroundTruthBoundariesMatrix As New Matrix(Of Byte)(oBoundaries)
                                                            CvInvoke.Threshold(oGroundTruthBoundariesMatrix, oGroundTruthBoundariesMatrix, 0, 255, CvEnum.ThresholdType.Binary)
                                                            SaveMatrix(sGroundTruthBoundariesName, oGroundTruthBoundariesMatrix)
                                                        End Using
                                                    End If
                                                End If

                                                Dim sGroundTruthSegmentationName As String = oFolderBrowserDialog.SelectedPath + GroundTruthSegmentationDirectory + sFileString + "[" + i.ToString + "].tif"
                                                If Not IO.File.Exists(sGroundTruthSegmentationName) Then
                                                    Dim oNodeSegmentation As Accord.IO.MatNode = oNode2.Fields("Segmentation")
                                                    If oNodeSegmentation.Value.GetType.Equals(GetType(System.UInt16(,))) Then
                                                        Dim oSegmentation As System.UInt16(,) = oNodeSegmentation.Value
                                                        Dim iHeight As Integer = oSegmentation.GetLength(0)
                                                        Dim iWidth As Integer = oSegmentation.GetLength(1)

                                                        ' create ground truth matrix if not present
                                                        Using oMatrixUShort As New Matrix(Of UShort)(oSegmentation)
                                                            Using oGroundTruthSegmentationMatrix As Matrix(Of Byte) = ConvertUShortLabels(oMatrixUShort)
                                                                SaveMatrix(sGroundTruthSegmentationName, oGroundTruthSegmentationMatrix)
                                                            End Using
                                                        End Using
                                                    End If
                                                End If
                                            End If
                                        Next
                                    End If
                                End Using

                                iCurrentProcessingGT += 1
                                Console.WriteLine(GetElapsed(oStartDate) + " Ground Truth " + iCurrentProcessingGT.ToString + "/" + iProcessingStepsGT.ToString + ": " + sFileString)
                            Next
                        End If
                    End If
#End Region

                    ' quantitative assessment of segmentation routines
#Region "Quantitative"
                    Console.WriteLine(GetElapsed(oStartDate) + " Quantitative Check Start")

                    ' -1 for any of the results indicates that no valid value was found
                    iCurrentProcessing = 0
                    iProcessingSteps = oImageFiles.Count * SuperpixelList.Count * [Enum].GetValues(GetType(SegmentType)).GetLength(0)
                    For Each oType In [Enum].GetValues(GetType(SegmentType))
                        Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                        Dim sQCFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "QC_" + sTypeName + ".xml"
                        Dim oQC As Quantitative = Nothing

                        If IO.File.Exists(sQCFile) Then
                            oQC = DeserializeDataContractFile(Of Quantitative)(sQCFile, Quantitative.GetKnownTypes, , , False)
                        Else
                            oQC = New Quantitative
                        End If

                        ' set boundary array
                        Dim oBRArray(4, 4) As Boolean
                        For yDisp = 0 To 4
                            For xDisp = 0 To 4
                                Dim fDistance As Single = Math.Sqrt((yDisp - 2) * (yDisp - 2) + (xDisp - 2) * (xDisp - 2))
                                If fDistance <= CSng(2) Then
                                    oBRArray(yDisp, xDisp) = True
                                Else
                                    oBRArray(yDisp, xDisp) = False
                                End If
                            Next
                        Next

                        Dim oGroundTruthBoundariesDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + GroundTruthBoundariesDirectory)
                        Dim oGroundTruthSegmentationDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + GroundTruthSegmentationDirectory)
                        For i = 0 To oImageFiles.Count - 1
                            Dim oImageFile As IO.FileInfo = oImageFiles(i)
                            If Not oQC.Results.ContainsKey(oImageFile.Name) Then
                                oQC.Results.Add(oImageFile.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Tuple(Of Single, Single, Single))))
                            End If
                            For Each iSuperpixel In SuperpixelList
                                If Not oQC.Results(oImageFile.Name).ContainsKey(iSuperpixel) Then
                                    oQC.Results(oImageFile.Name).Add(iSuperpixel, New Dictionary(Of SegmentType, Tuple(Of Single, Single, Single)))
                                End If
                                If (Not oQC.Results(oImageFile.Name)(iSuperpixel).ContainsKey(oType)) Then
                                    Dim oOutlineOnlyImageFile As New IO.FileInfo(oFolderBrowserDialog.SelectedPath + OutlineImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iSuperpixel.ToString + "].tif")
                                    Dim oSegmentationImageFile As New IO.FileInfo(oFolderBrowserDialog.SelectedPath + SegmentationImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iSuperpixel.ToString + "].tif")
                                    If oOutlineOnlyImageFile.Exists AndAlso oSegmentationImageFile.Exists Then
                                        Dim oBRList As New List(Of Single)
                                        Dim oGroundTruthBoundariesFiles As List(Of IO.FileInfo) = oGroundTruthBoundariesDirectoryInfo.EnumerateFiles(Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[*].tif", IO.SearchOption.TopDirectoryOnly).ToList

                                        Using oOutlineOnlyBitmap As New System.Drawing.Bitmap(oOutlineOnlyImageFile.FullName)
                                            Using oOutlineOnlyMatrix As Matrix(Of Byte) = BitmapToMatrix(oOutlineOnlyBitmap)
                                                Using oMonoOutlineOnlyMatrix As New Matrix(Of Byte)(oOutlineOnlyMatrix.Size)
                                                    CvInvoke.CvtColor(oOutlineOnlyMatrix, oMonoOutlineOnlyMatrix, CvEnum.ColorConversion.Bgr2Gray)
                                                    Dim iOutlineOnlyWidth As Integer = oOutlineOnlyMatrix.Width
                                                    Dim iOutlineOnlyHeight As Integer = oOutlineOnlyMatrix.Height
                                                    Dim oOutlineRect As New System.Drawing.Rectangle(0, 0, iOutlineOnlyWidth, iOutlineOnlyHeight)

                                                    For Each oGroundTruthBoundariesFile In oGroundTruthBoundariesFiles
                                                        Dim iTotalPixels As Integer = 0
                                                        Dim iPixelsIn As Integer = 0
                                                        Using oGroundTruthBoundariesBitmap As New System.Drawing.Bitmap(oGroundTruthBoundariesFile.FullName)
                                                            Using oGroundTruthBoundariesMatrix As Matrix(Of Byte) = BitmapToMatrix(oGroundTruthBoundariesBitmap)
                                                                Using oMonoGroundTruthBoundariesMatrix As New Matrix(Of Byte)(oGroundTruthBoundariesMatrix.Size)
                                                                    CvInvoke.CvtColor(oGroundTruthBoundariesMatrix, oMonoGroundTruthBoundariesMatrix, CvEnum.ColorConversion.Bgr2Gray)

                                                                    Dim TaskDelegateGT As Action(Of Object) = Sub(ByVal oParam As Tuple(Of Integer, Integer))
                                                                                                                  Dim y As Integer = oParam.Item2
                                                                                                                  For x = 0 To iOutlineOnlyWidth - 1
                                                                                                                      If oMonoGroundTruthBoundariesMatrix(y, x) <> 0 Then
                                                                                                                          System.Threading.Interlocked.Increment(iTotalPixels)
                                                                                                                          Dim bPixelIn As Boolean = False
                                                                                                                          For yDisp = -2 To 2
                                                                                                                              For xDisp = -2 To 2
                                                                                                                                  If oBRArray(yDisp + 2, xDisp + 2) Then
                                                                                                                                      Dim yComb As Integer = y + yDisp
                                                                                                                                      Dim xComb As Integer = x + xDisp

                                                                                                                                      If oOutlineRect.Contains(xComb, yComb) Then
                                                                                                                                          If oMonoOutlineOnlyMatrix(yComb, xComb) <> 0 Then
                                                                                                                                              bPixelIn = True
                                                                                                                                          End If
                                                                                                                                      End If
                                                                                                                                  End If
                                                                                                                                  If bPixelIn Then
                                                                                                                                      Exit For
                                                                                                                                  End If
                                                                                                                              Next
                                                                                                                              If bPixelIn Then
                                                                                                                                  Exit For
                                                                                                                              End If
                                                                                                                          Next
                                                                                                                          If bPixelIn Then
                                                                                                                              System.Threading.Interlocked.Increment(iPixelsIn)
                                                                                                                          End If
                                                                                                                      End If
                                                                                                                  Next
                                                                                                              End Sub

                                                                    ParallelProcess(Enumerable.Range(0, iOutlineOnlyHeight).ToList, TaskDelegateGT, AddressOf UpdateTasks, AddressOf CPUUtilisation, 4, 8)
                                                                End Using
                                                            End Using
                                                        End Using

                                                        oBRList.Add(CSng(iPixelsIn) / CSng(iTotalPixels))
                                                    Next
                                                End Using
                                            End Using
                                        End Using

                                        Dim oUEList As New ConcurrentBag(Of Single)
                                        Dim oASAList As New ConcurrentBag(Of Single)
                                        Dim oGroundTruthSegmentationFiles As List(Of IO.FileInfo) = oGroundTruthSegmentationDirectoryInfo.EnumerateFiles(Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[*].tif", IO.SearchOption.TopDirectoryOnly).ToList
                                        Using oSegmentationBitmap As New System.Drawing.Bitmap(oSegmentationImageFile.FullName)
                                            Using oSegmentationMatrix As Matrix(Of Byte) = BitmapToMatrix(oSegmentationBitmap)
                                                Using oLabelsSegmentationMatrix As Matrix(Of UShort) = UnconvertUShortLabels(oSegmentationMatrix)
                                                    Dim iSegmentationWidth As Integer = oSegmentationMatrix.Width
                                                    Dim iSegmentationHeight As Integer = oSegmentationMatrix.Height

                                                    ' create segment dictionary of point locations
                                                    Dim oSegmentDictionary As New Dictionary(Of Integer, List(Of System.Drawing.Point))
                                                    For y = 0 To iSegmentationHeight - 1
                                                        For x = 0 To iSegmentationWidth - 1
                                                            Dim iLabel As UShort = oLabelsSegmentationMatrix(y, x)
                                                            If Not oSegmentDictionary.ContainsKey(iLabel) Then
                                                                oSegmentDictionary.Add(iLabel, New List(Of System.Drawing.Point))
                                                            End If

                                                            oSegmentDictionary(iLabel).Add(New System.Drawing.Point(x, y))
                                                        Next
                                                    Next

                                                    Dim oCombineAction As Action(Of Object) = Sub(oParam As IO.FileInfo)
                                                                                                  Using oGroundTruthSegmentationBitmap As New System.Drawing.Bitmap(oParam.FullName)
                                                                                                      Using oGroundTruthSegmentationMatrix As Matrix(Of Byte) = BitmapToMatrix(oGroundTruthSegmentationBitmap)
                                                                                                          Using oLabelsGroundTruthSegmentationMatrix As Matrix(Of UShort) = UnconvertUShortLabels(oGroundTruthSegmentationMatrix)
                                                                                                              Dim oOverlapDictionary As New Dictionary(Of Integer, Dictionary(Of Integer, Integer))
                                                                                                              For Each oKeyValue In oSegmentDictionary
                                                                                                                  If Not oOverlapDictionary.ContainsKey(oKeyValue.Key) Then
                                                                                                                      oOverlapDictionary.Add(oKeyValue.Key, New Dictionary(Of Integer, Integer))
                                                                                                                  End If

                                                                                                                  For Each oPoint In oKeyValue.Value
                                                                                                                      Dim iLabel As UShort = oLabelsGroundTruthSegmentationMatrix(oPoint.Y, oPoint.X)
                                                                                                                      If Not oOverlapDictionary(oKeyValue.Key).ContainsKey(iLabel) Then
                                                                                                                          oOverlapDictionary(oKeyValue.Key).Add(iLabel, 0)
                                                                                                                      End If
                                                                                                                      oOverlapDictionary(oKeyValue.Key)(iLabel) += 1
                                                                                                                  Next
                                                                                                              Next

                                                                                                              Dim oOrderedDictionary As New Dictionary(Of Integer, List(Of KeyValuePair(Of Integer, Integer)))
                                                                                                              For Each oKeyValue In oOverlapDictionary
                                                                                                                  If Not oOrderedDictionary.ContainsKey(oKeyValue.Key) Then
                                                                                                                      oOrderedDictionary.Add(oKeyValue.Key, oKeyValue.Value.ToList.OrderByDescending(Function(x) x.Value).ToList)
                                                                                                                  End If
                                                                                                              Next

                                                                                                              Dim iTotalPoints As Integer = iSegmentationWidth * iSegmentationHeight
                                                                                                              Dim iUEPoints As Integer = 0
                                                                                                              Dim iASAPoints As Integer = 0
                                                                                                              For Each oKeyValue In oOrderedDictionary
                                                                                                                  If oKeyValue.Value.Count = 1 Then
                                                                                                                      ' if superpixel is completely contained within segment, then add all points to the ASA count
                                                                                                                      iASAPoints += oKeyValue.Value.First.Value
                                                                                                                  Else
                                                                                                                      ' add the smaller groups to the UE count
                                                                                                                      For j = 1 To oKeyValue.Value.Count - 1
                                                                                                                          iUEPoints += oKeyValue.Value(j).Value
                                                                                                                      Next

                                                                                                                      ' add the largest group to the ASA count
                                                                                                                      iASAPoints += oKeyValue.Value.First.Value
                                                                                                                  End If
                                                                                                              Next

                                                                                                              oUEList.Add(CSng(iUEPoints) / CSng(iTotalPoints))
                                                                                                              oASAList.Add(CSng(iASAPoints) / CSng(iTotalPoints))
                                                                                                          End Using
                                                                                                      End Using
                                                                                                  End Using
                                                                                              End Sub

                                                    Dim oCombineActionList As New List(Of Tuple(Of Action(Of Object), Object))
                                                    For Each oGroundTruthSegmentationFile In oGroundTruthSegmentationFiles
                                                        oCombineActionList.Add(New Tuple(Of Action(Of Object), Object)(oCombineAction, oGroundTruthSegmentationFile))
                                                    Next

                                                    ProtectedRunTasks(oCombineActionList)
                                                End Using
                                            End Using
                                        End Using

                                        Dim fBR As Single = If(oBRList.Count = 0, -1, oBRList.Average)
                                        Dim fUE As Single = If(oUEList.Count = 0, -1, oUEList.Average)
                                        Dim fASA As Single = If(oASAList.Count = 0, -1, oASAList.Average)

                                        oQC.Results(oImageFile.Name)(iSuperpixel).Add(oType, New Tuple(Of Single, Single, Single)(fBR, fUE, fASA))
                                    Else
                                        oQC.Results(oImageFile.Name)(iSuperpixel).Add(oType, New Tuple(Of Single, Single, Single)(-1, -1, -1))
                                    End If
                                End If

                                iCurrentProcessing += 1
                                Console.WriteLine(GetElapsed(oStartDate) + " QC Processing " + iCurrentProcessing.ToString + "/" + iProcessingSteps.ToString)
                            Next
                        Next

                        SerializeDataContractFile(sQCFile, oQC, Quantitative.GetKnownTypes, , , False)

                        Console.WriteLine(GetElapsed(oStartDate) + " Quantitative Check " + sTypeName + " Saved")
                    Next
#End Region

                    ' quantitative assessment of noise routines
#Region "Quantitative Noise"
                    Console.WriteLine(GetElapsed(oStartDate) + " Quantitative Noise Check Start")

                    ' -1 for any of the results indicates that no valid value was found
                    iCurrentProcessing = 0
                    iProcessingSteps = oImageFiles.Count * NoiseList.Count * [Enum].GetValues(GetType(SegmentType)).GetLength(0)
                    For Each oType In [Enum].GetValues(GetType(SegmentType))
                        Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                        Dim sQCNoiseFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "QCNoise_" + sTypeName + ".xml"
                        Dim oQCNoise As Quantitative = Nothing

                        If IO.File.Exists(sQCNoiseFile) Then
                            oQCNoise = DeserializeDataContractFile(Of Quantitative)(sQCNoiseFile, Quantitative.GetKnownTypes, , , False)
                        Else
                            oQCNoise = New Quantitative
                        End If

                        ' set boundary array
                        Dim oBRArray(4, 4) As Boolean
                        For yDisp = 0 To 4
                            For xDisp = 0 To 4
                                Dim fDistance As Single = Math.Sqrt((yDisp - 2) * (yDisp - 2) + (xDisp - 2) * (xDisp - 2))
                                If fDistance <= CSng(2) Then
                                    oBRArray(yDisp, xDisp) = True
                                Else
                                    oBRArray(yDisp, xDisp) = False
                                End If
                            Next
                        Next

                        Dim oGroundTruthBoundariesDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + GroundTruthBoundariesDirectory)
                        Dim oGroundTruthSegmentationDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + GroundTruthSegmentationDirectory)
                        For i = 0 To oImageFiles.Count - 1
                            Dim oImageFile As IO.FileInfo = oImageFiles(i)
                            If Not oQCNoise.Results.ContainsKey(oImageFile.Name) Then
                                oQCNoise.Results.Add(oImageFile.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Tuple(Of Single, Single, Single))))
                            End If
                            For Each fNoise In NoiseList
                                Dim iNoise As Integer = CInt(fNoise * CSng(NoiseMul))
                                If Not oQCNoise.Results(oImageFile.Name).ContainsKey(iNoise) Then
                                    oQCNoise.Results(oImageFile.Name).Add(iNoise, New Dictionary(Of SegmentType, Tuple(Of Single, Single, Single)))
                                End If
                                If (Not oQCNoise.Results(oImageFile.Name)(iNoise).ContainsKey(oType)) Then
                                    Dim oOutlineOnlyImageFile As New IO.FileInfo(oFolderBrowserDialog.SelectedPath + NoiseOutlineImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iNoise.ToString + "].tif")
                                    Dim oSegmentationImageFile As New IO.FileInfo(oFolderBrowserDialog.SelectedPath + NoiseSegmentationImagesDirectory + sTypeName + "\" + Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[" + sTypeName + "][" + iNoise.ToString + "].tif")
                                    If oOutlineOnlyImageFile.Exists AndAlso oSegmentationImageFile.Exists Then
                                        Dim oBRList As New List(Of Single)
                                        Dim oGroundTruthBoundariesFiles As List(Of IO.FileInfo) = oGroundTruthBoundariesDirectoryInfo.EnumerateFiles(Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[*].tif", IO.SearchOption.TopDirectoryOnly).ToList

                                        Using oOutlineOnlyBitmap As New System.Drawing.Bitmap(oOutlineOnlyImageFile.FullName)
                                            Using oOutlineOnlyMatrix As Matrix(Of Byte) = BitmapToMatrix(oOutlineOnlyBitmap)
                                                Using oMonoOutlineOnlyMatrix As New Matrix(Of Byte)(oOutlineOnlyMatrix.Size)
                                                    CvInvoke.CvtColor(oOutlineOnlyMatrix, oMonoOutlineOnlyMatrix, CvEnum.ColorConversion.Bgr2Gray)
                                                    Dim iOutlineOnlyWidth As Integer = oOutlineOnlyMatrix.Width
                                                    Dim iOutlineOnlyHeight As Integer = oOutlineOnlyMatrix.Height
                                                    Dim oOutlineRect As New System.Drawing.Rectangle(0, 0, iOutlineOnlyWidth, iOutlineOnlyHeight)

                                                    For Each oGroundTruthBoundariesFile In oGroundTruthBoundariesFiles
                                                        Dim iTotalPixels As Integer = 0
                                                        Dim iPixelsIn As Integer = 0
                                                        Using oGroundTruthBoundariesBitmap As New System.Drawing.Bitmap(oGroundTruthBoundariesFile.FullName)
                                                            Using oGroundTruthBoundariesMatrix As Matrix(Of Byte) = BitmapToMatrix(oGroundTruthBoundariesBitmap)
                                                                Using oMonoGroundTruthBoundariesMatrix As New Matrix(Of Byte)(oGroundTruthBoundariesMatrix.Size)
                                                                    CvInvoke.CvtColor(oGroundTruthBoundariesMatrix, oMonoGroundTruthBoundariesMatrix, CvEnum.ColorConversion.Bgr2Gray)

                                                                    Dim TaskDelegateGT As Action(Of Object) = Sub(ByVal oParam As Tuple(Of Integer, Integer))
                                                                                                                  Dim y As Integer = oParam.Item2
                                                                                                                  For x = 0 To iOutlineOnlyWidth - 1
                                                                                                                      If oMonoGroundTruthBoundariesMatrix(y, x) <> 0 Then
                                                                                                                          System.Threading.Interlocked.Increment(iTotalPixels)
                                                                                                                          Dim bPixelIn As Boolean = False
                                                                                                                          For yDisp = -2 To 2
                                                                                                                              For xDisp = -2 To 2
                                                                                                                                  If oBRArray(yDisp + 2, xDisp + 2) Then
                                                                                                                                      Dim yComb As Integer = y + yDisp
                                                                                                                                      Dim xComb As Integer = x + xDisp

                                                                                                                                      If oOutlineRect.Contains(xComb, yComb) Then
                                                                                                                                          If oMonoOutlineOnlyMatrix(yComb, xComb) <> 0 Then
                                                                                                                                              bPixelIn = True
                                                                                                                                          End If
                                                                                                                                      End If
                                                                                                                                  End If
                                                                                                                                  If bPixelIn Then
                                                                                                                                      Exit For
                                                                                                                                  End If
                                                                                                                              Next
                                                                                                                              If bPixelIn Then
                                                                                                                                  Exit For
                                                                                                                              End If
                                                                                                                          Next
                                                                                                                          If bPixelIn Then
                                                                                                                              System.Threading.Interlocked.Increment(iPixelsIn)
                                                                                                                          End If
                                                                                                                      End If
                                                                                                                  Next
                                                                                                              End Sub

                                                                    ParallelProcess(Enumerable.Range(0, iOutlineOnlyHeight).ToList, TaskDelegateGT, AddressOf UpdateTasks, AddressOf CPUUtilisation, 4, 8)
                                                                End Using
                                                            End Using
                                                        End Using

                                                        oBRList.Add(CSng(iPixelsIn) / CSng(iTotalPixels))
                                                    Next
                                                End Using
                                            End Using
                                        End Using

                                        Dim oUEList As New ConcurrentBag(Of Single)
                                        Dim oASAList As New ConcurrentBag(Of Single)
                                        Dim oGroundTruthSegmentationFiles As List(Of IO.FileInfo) = oGroundTruthSegmentationDirectoryInfo.EnumerateFiles(Left(oImageFile.Name, Len(oImageFile.Name) - Len(oImageFile.Extension)) + "[*].tif", IO.SearchOption.TopDirectoryOnly).ToList
                                        Using oSegmentationBitmap As New System.Drawing.Bitmap(oSegmentationImageFile.FullName)
                                            Using oSegmentationMatrix As Matrix(Of Byte) = BitmapToMatrix(oSegmentationBitmap)
                                                Using oLabelsSegmentationMatrix As Matrix(Of UShort) = UnconvertUShortLabels(oSegmentationMatrix)
                                                    Dim iSegmentationWidth As Integer = oSegmentationMatrix.Width
                                                    Dim iSegmentationHeight As Integer = oSegmentationMatrix.Height

                                                    ' create segment dictionary of point locations
                                                    Dim oSegmentDictionary As New Dictionary(Of Integer, List(Of System.Drawing.Point))
                                                    For y = 0 To iSegmentationHeight - 1
                                                        For x = 0 To iSegmentationWidth - 1
                                                            Dim iLabel As UShort = oLabelsSegmentationMatrix(y, x)
                                                            If Not oSegmentDictionary.ContainsKey(iLabel) Then
                                                                oSegmentDictionary.Add(iLabel, New List(Of System.Drawing.Point))
                                                            End If

                                                            oSegmentDictionary(iLabel).Add(New System.Drawing.Point(x, y))
                                                        Next
                                                    Next

                                                    Dim oCombineAction As Action(Of Object) = Sub(oParam As IO.FileInfo)
                                                                                                  Using oGroundTruthSegmentationBitmap As New System.Drawing.Bitmap(oParam.FullName)
                                                                                                      Using oGroundTruthSegmentationMatrix As Matrix(Of Byte) = BitmapToMatrix(oGroundTruthSegmentationBitmap)
                                                                                                          Using oLabelsGroundTruthSegmentationMatrix As Matrix(Of UShort) = UnconvertUShortLabels(oGroundTruthSegmentationMatrix)
                                                                                                              Dim oOverlapDictionary As New Dictionary(Of Integer, Dictionary(Of Integer, Integer))
                                                                                                              For Each oKeyValue In oSegmentDictionary
                                                                                                                  If Not oOverlapDictionary.ContainsKey(oKeyValue.Key) Then
                                                                                                                      oOverlapDictionary.Add(oKeyValue.Key, New Dictionary(Of Integer, Integer))
                                                                                                                  End If

                                                                                                                  For Each oPoint In oKeyValue.Value
                                                                                                                      Dim iLabel As UShort = oLabelsGroundTruthSegmentationMatrix(oPoint.Y, oPoint.X)
                                                                                                                      If Not oOverlapDictionary(oKeyValue.Key).ContainsKey(iLabel) Then
                                                                                                                          oOverlapDictionary(oKeyValue.Key).Add(iLabel, 0)
                                                                                                                      End If
                                                                                                                      oOverlapDictionary(oKeyValue.Key)(iLabel) += 1
                                                                                                                  Next
                                                                                                              Next

                                                                                                              Dim oOrderedDictionary As New Dictionary(Of Integer, List(Of KeyValuePair(Of Integer, Integer)))
                                                                                                              For Each oKeyValue In oOverlapDictionary
                                                                                                                  If Not oOrderedDictionary.ContainsKey(oKeyValue.Key) Then
                                                                                                                      oOrderedDictionary.Add(oKeyValue.Key, oKeyValue.Value.ToList.OrderByDescending(Function(x) x.Value).ToList)
                                                                                                                  End If
                                                                                                              Next

                                                                                                              Dim iTotalPoints As Integer = iSegmentationWidth * iSegmentationHeight
                                                                                                              Dim iUEPoints As Integer = 0
                                                                                                              Dim iASAPoints As Integer = 0
                                                                                                              For Each oKeyValue In oOrderedDictionary
                                                                                                                  If oKeyValue.Value.Count = 1 Then
                                                                                                                      ' if superpixel is completely contained within segment, then add all points to the ASA count
                                                                                                                      iASAPoints += oKeyValue.Value.First.Value
                                                                                                                  Else
                                                                                                                      ' add the smaller groups to the UE count
                                                                                                                      For j = 1 To oKeyValue.Value.Count - 1
                                                                                                                          iUEPoints += oKeyValue.Value(j).Value
                                                                                                                      Next

                                                                                                                      ' add the largest group to the ASA count
                                                                                                                      iASAPoints += oKeyValue.Value.First.Value
                                                                                                                  End If
                                                                                                              Next

                                                                                                              oUEList.Add(CSng(iUEPoints) / CSng(iTotalPoints))
                                                                                                              oASAList.Add(CSng(iASAPoints) / CSng(iTotalPoints))
                                                                                                          End Using
                                                                                                      End Using
                                                                                                  End Using
                                                                                              End Sub

                                                    Dim oCombineActionList As New List(Of Tuple(Of Action(Of Object), Object))
                                                    For Each oGroundTruthSegmentationFile In oGroundTruthSegmentationFiles
                                                        oCombineActionList.Add(New Tuple(Of Action(Of Object), Object)(oCombineAction, oGroundTruthSegmentationFile))
                                                    Next

                                                    ProtectedRunTasks(oCombineActionList)
                                                End Using
                                            End Using
                                        End Using

                                        Dim fBR As Single = If(oBRList.Count = 0, -1, oBRList.Average)
                                        Dim fUE As Single = If(oUEList.Count = 0, -1, oUEList.Average)
                                        Dim fASA As Single = If(oASAList.Count = 0, -1, oASAList.Average)

                                        oQCNoise.Results(oImageFile.Name)(iNoise).Add(oType, New Tuple(Of Single, Single, Single)(fBR, fUE, fASA))
                                    Else
                                        oQCNoise.Results(oImageFile.Name)(iNoise).Add(oType, New Tuple(Of Single, Single, Single)(-1, -1, -1))
                                    End If
                                End If

                                iCurrentProcessing += 1
                                Console.WriteLine(GetElapsed(oStartDate) + " QCNoise Processing " + iCurrentProcessing.ToString + "/" + iProcessingSteps.ToString)
                            Next
                        Next

                        SerializeDataContractFile(sQCNoiseFile, oQCNoise, Quantitative.GetKnownTypes, , , False)

                        Console.WriteLine(GetElapsed(oStartDate) + " Quantitative Check " + sTypeName + " Saved")
                    Next
#End Region

                    ' export results to Excel
#Region "Report"
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial
                    Using oDataDocument As New ExcelPackage()
                        oDataDocument.Workbook.Worksheets.Add(DataStr)
                        oDataDocument.Workbook.Worksheets.Add(ComplexityStr)
                        oDataDocument.Workbook.Worksheets.Add(AltComplexityStr)
                        oDataDocument.Workbook.Worksheets.Add(NoiseStr)
                        Using oDataSheet As ExcelWorksheet = oDataDocument.Workbook.Worksheets(DataStr)
                            Using oComplexitySheet As ExcelWorksheet = oDataDocument.Workbook.Worksheets(ComplexityStr)
                                Using oAltComplexitySheet As ExcelWorksheet = oDataDocument.Workbook.Worksheets(AltComplexityStr)
                                    Using oNoiseSheet As ExcelWorksheet = oDataDocument.Workbook.Worksheets(NoiseStr)
                                        Const iNumColData As Integer = 1
                                        Const iTypeColData As Integer = 2
                                        Const iSuperpixelsColData As Integer = 3
                                        Const iSegmentsColData As Integer = 4
                                        Const iTimeColData As Integer = 5
                                        Const iBPColData As Integer = 6
                                        Const iUEColData As Integer = 7
                                        Const iASAColData As Integer = 8

                                        oDataSheet.SetValue(1, iNumColData, NumStr)
                                        oDataSheet.SetValue(1, iTypeColData, TypeStr)
                                        oDataSheet.SetValue(1, iSuperpixelsColData, SuperpixelsStr)
                                        oDataSheet.SetValue(1, iSegmentsColData, SegmentsStr)
                                        oDataSheet.SetValue(1, iTimeColData, TimeStr)
                                        oDataSheet.SetValue(1, iBPColData, BPStr)
                                        oDataSheet.SetValue(1, iUEColData, UEStr)
                                        oDataSheet.SetValue(1, iASAColData, ASAStr)

                                        Dim iCurrentRow As Integer = 2
                                        For Each oType In [Enum].GetValues(GetType(SegmentType))
                                            Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                                            Dim sQCFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "QC_" + sTypeName + ".xml"
                                            Dim oQC As Quantitative = Nothing

                                            Dim sTimingsFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Timings_" + sTypeName + ".xml"
                                            Dim oTimings As Result = Nothing

                                            Dim sSegmentsFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Segments_" + sTypeName + ".xml"
                                            Dim oSegments As Result = Nothing

                                            If IO.File.Exists(sTimingsFile) AndAlso IO.File.Exists(sSegmentsFile) AndAlso IO.File.Exists(sQCFile) Then
                                                oTimings = DeserializeDataContractFile(Of Result)(sTimingsFile, Result.GetKnownTypes, , , False)
                                                oSegments = DeserializeDataContractFile(Of Result)(sSegmentsFile, Result.GetKnownTypes, , , False)
                                                oQC = DeserializeDataContractFile(Of Quantitative)(sQCFile, Quantitative.GetKnownTypes, , , False)

                                                For Each iSuperpixel In SuperpixelList
                                                    Dim iCount As Integer = 0
                                                    Dim iTimeTotal As Integer = 0
                                                    Dim iSegmentsTotal As Integer = 0
                                                    Dim fBPTotal As Single = 0
                                                    Dim fUETotal As Single = 0
                                                    Dim fASATotal As Single = 0

                                                    For Each sFileName In oQC.Results.Keys
                                                        Dim oQCResult As Tuple(Of Single, Single, Single) = oQC.Results(sFileName)(iSuperpixel)(oType)
                                                        Dim iTime As Integer = oTimings.Results(sFileName)(iSuperpixel)(oType)
                                                        Dim iSegments As Integer = oSegments.Results(sFileName)(iSuperpixel)(oType)

                                                        iCount += 1
                                                        iTimeTotal += iTime
                                                        iSegmentsTotal += iSegments
                                                        fBPTotal += oQCResult.Item1
                                                        fUETotal += oQCResult.Item2
                                                        fASATotal += oQCResult.Item3
                                                    Next

                                                    oDataSheet.SetValue(iCurrentRow, iNumColData, iCurrentRow - 1)
                                                    oDataSheet.SetValue(iCurrentRow, iTypeColData, sTypeName)
                                                    oDataSheet.SetValue(iCurrentRow, iSuperpixelsColData, iSuperpixel)
                                                    oDataSheet.SetValue(iCurrentRow, iSegmentsColData, CInt(CSng(iSegmentsTotal) / CSng(iCount)))
                                                    oDataSheet.SetValue(iCurrentRow, iTimeColData, CInt(CSng(iTimeTotal) / CSng(iCount)))
                                                    oDataSheet.SetValue(iCurrentRow, iBPColData, fBPTotal / CSng(iCount))
                                                    oDataSheet.SetValue(iCurrentRow, iUEColData, fUETotal / CSng(iCount))
                                                    oDataSheet.SetValue(iCurrentRow, iASAColData, fASATotal / CSng(iCount))
                                                    iCurrentRow += 1
                                                Next
                                            End If
                                        Next

                                        ' autofit columns
                                        oDataSheet.Cells(oDataSheet.Dimension.Start.Row, oDataSheet.Dimension.Start.Column, oDataSheet.Dimension.End.Row, oDataSheet.Dimension.End.Column).AutoFitColumns()

                                        Const iNumColComplexity As Integer = 1
                                        Const iTypeColComplexity As Integer = 2
                                        Const iComplexityColComplexity As Integer = 3
                                        Const iTimeColComplexity As Integer = 4

                                        oComplexitySheet.SetValue(1, iNumColComplexity, NumStr)
                                        oComplexitySheet.SetValue(1, iTypeColComplexity, TypeStr)
                                        oComplexitySheet.SetValue(1, iComplexityColComplexity, ComplexityStr)
                                        oComplexitySheet.SetValue(1, iTimeColComplexity, TimeStr)

                                        iCurrentRow = 2
                                        For Each oType In [Enum].GetValues(GetType(SegmentType))
                                            If oType <> SegmentType.DSFH Then
                                                Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                                                Dim sComplexityFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "Complexity_" + sTypeName + ".xml"
                                                Dim oComplexity As Result = Nothing

                                                If IO.File.Exists(sComplexityFile) Then
                                                    oComplexity = DeserializeDataContractFile(Of Result)(sComplexityFile, Result.GetKnownTypes, , , False)

                                                    For Each fComplexity In ComplexityList
                                                        Dim iComplexity As Integer = CInt(fComplexity * CSng(ComplexityMul))
                                                        Dim iCount As Integer = 0
                                                        Dim iTimeTotal As Integer = 0

                                                        For Each sFileName In oComplexity.Results.Keys
                                                            Dim iTime As Integer = oComplexity.Results(sFileName)(iComplexity)(oType)

                                                            iCount += 1
                                                            iTimeTotal += iTime
                                                        Next

                                                        oComplexitySheet.SetValue(iCurrentRow, iNumColComplexity, iCurrentRow - 1)
                                                        oComplexitySheet.SetValue(iCurrentRow, iTypeColComplexity, sTypeName)
                                                        oComplexitySheet.SetValue(iCurrentRow, iComplexityColComplexity, fComplexity)
                                                        oComplexitySheet.SetValue(iCurrentRow, iTimeColComplexity, CInt(CSng(iTimeTotal) / CSng(iCount)))
                                                        iCurrentRow += 1
                                                    Next
                                                End If
                                            End If
                                        Next

                                        ' autofit columns
                                        oComplexitySheet.Cells(oComplexitySheet.Dimension.Start.Row, oComplexitySheet.Dimension.Start.Column, oComplexitySheet.Dimension.End.Row, oComplexitySheet.Dimension.End.Column).AutoFitColumns()

                                        oAltComplexitySheet.SetValue(1, iNumColComplexity, NumStr)
                                        oAltComplexitySheet.SetValue(1, iTypeColComplexity, TypeStr)
                                        oAltComplexitySheet.SetValue(1, iComplexityColComplexity, ComplexityStr)
                                        oAltComplexitySheet.SetValue(1, iTimeColComplexity, TimeStr)

                                        iCurrentRow = 2
                                        For Each oType In [Enum].GetValues(GetType(SegmentType))
                                            If oType <> SegmentType.DSFH Then
                                                Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                                                Dim sAltComplexityFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "AltComplexity_" + sTypeName + ".xml"
                                                Dim oAltComplexity As Result = Nothing

                                                If IO.File.Exists(sAltComplexityFile) Then
                                                    oAltComplexity = DeserializeDataContractFile(Of Result)(sAltComplexityFile, Result.GetKnownTypes, , , False)

                                                    For Each fAltComplexity In ComplexityList
                                                        Dim fComplexity As Single = fAltComplexity / 8.0
                                                        Dim iComplexity As Integer = CInt(fComplexity * CSng(ComplexityMul))
                                                        Dim iCount As Integer = 0
                                                        Dim iTimeTotal As Integer = 0

                                                        For Each sFileName In oAltComplexity.Results.Keys
                                                            Dim iTime As Integer = oAltComplexity.Results(sFileName)(iComplexity)(oType)

                                                            iCount += 1
                                                            iTimeTotal += iTime
                                                        Next

                                                        oAltComplexitySheet.SetValue(iCurrentRow, iNumColComplexity, iCurrentRow - 1)
                                                        oAltComplexitySheet.SetValue(iCurrentRow, iTypeColComplexity, sTypeName)
                                                        oAltComplexitySheet.SetValue(iCurrentRow, iComplexityColComplexity, fComplexity)
                                                        oAltComplexitySheet.SetValue(iCurrentRow, iTimeColComplexity, CInt(CSng(iTimeTotal) / CSng(iCount)))
                                                        iCurrentRow += 1
                                                    Next
                                                End If
                                            End If
                                        Next

                                        ' autofit columns
                                        oAltComplexitySheet.Cells(oAltComplexitySheet.Dimension.Start.Row, oAltComplexitySheet.Dimension.Start.Column, oAltComplexitySheet.Dimension.End.Row, oAltComplexitySheet.Dimension.End.Column).AutoFitColumns()

                                        Const iNumColNoise As Integer = 1
                                        Const iTypeColNoise As Integer = 2
                                        Const iNoiseColNoise As Integer = 3
                                        Const iBPColNoise As Integer = 4
                                        Const iUEColNoise As Integer = 5
                                        Const iASAColNoise As Integer = 6

                                        oNoiseSheet.SetValue(1, iNumColNoise, NumStr)
                                        oNoiseSheet.SetValue(1, iTypeColNoise, TypeStr)
                                        oNoiseSheet.SetValue(1, iNoiseColNoise, NoiseStr)
                                        oNoiseSheet.SetValue(1, iBPColNoise, BPStr)
                                        oNoiseSheet.SetValue(1, iUEColNoise, UEStr)
                                        oNoiseSheet.SetValue(1, iASAColNoise, ASAStr)

                                        iCurrentRow = 2
                                        For Each oType In [Enum].GetValues(GetType(SegmentType))
                                            Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)

                                            Dim sQCNoiseFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "QCNoise_" + sTypeName + ".xml"
                                            Dim oQCNoise As Quantitative = Nothing

                                            If IO.File.Exists(sQCNoiseFile) Then
                                                oQCNoise = DeserializeDataContractFile(Of Quantitative)(sQCNoiseFile, Quantitative.GetKnownTypes, , , False)

                                                For Each fNoise In NoiseList
                                                    Dim iNoise As Integer = CInt(fNoise * CSng(NoiseMul))
                                                    Dim iCount As Integer = 0
                                                    Dim fBPTotal As Single = 0
                                                    Dim fUETotal As Single = 0
                                                    Dim fASATotal As Single = 0

                                                    For Each sFileName In oQCNoise.Results.Keys
                                                        Dim oQCNoiseResult As Tuple(Of Single, Single, Single) = oQCNoise.Results(sFileName)(iNoise)(oType)

                                                        iCount += 1
                                                        fBPTotal += oQCNoiseResult.Item1
                                                        fUETotal += oQCNoiseResult.Item2
                                                        fASATotal += oQCNoiseResult.Item3
                                                    Next

                                                    oNoiseSheet.SetValue(iCurrentRow, iNumColNoise, iCurrentRow - 1)
                                                    oNoiseSheet.SetValue(iCurrentRow, iTypeColNoise, sTypeName)
                                                    oNoiseSheet.SetValue(iCurrentRow, iNoiseColNoise, fNoise)
                                                    oNoiseSheet.SetValue(iCurrentRow, iBPColNoise, fBPTotal / CSng(iCount))
                                                    oNoiseSheet.SetValue(iCurrentRow, iUEColNoise, fUETotal / CSng(iCount))
                                                    oNoiseSheet.SetValue(iCurrentRow, iASAColNoise, fASATotal / CSng(iCount))
                                                    iCurrentRow += 1
                                                Next
                                            End If
                                        Next

                                        ' autofit columns
                                        oNoiseSheet.Cells(oNoiseSheet.Dimension.Start.Row, oNoiseSheet.Dimension.Start.Column, oNoiseSheet.Dimension.End.Row, oNoiseSheet.Dimension.End.Column).AutoFitColumns()

                                        Dim sDataFile As String = oFolderBrowserDialog.SelectedPath + SaveDirectory + "ScanSegmentData.xlsx"
                                        Dim oDataInfo As New IO.FileInfo(sDataFile)
                                        If oDataInfo.Exists Then
                                            oDataInfo.Delete()
                                        End If

                                        Console.WriteLine(GetElapsed(oStartDate) + " Saving File " + oDataInfo.Name)
                                        oDataDocument.SaveAs(oDataInfo)
                                    End Using
                                End Using
                            End Using
                        End Using
                    End Using
#End Region
                End If
            End If
        End If
    End Sub
    Private Sub ProcessCombinedMultiplier(ByVal oImageFiles As List(Of IO.FileInfo), ByVal oLargeMul As List(Of Integer), ByVal oType As SegmentType, ByVal oStartDate As Date, ByVal oSelectedPath As String)
        Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)
        Dim sTypeMultiplierFile As String = oSelectedPath + SaveDirectory + "Multipliers_" + sTypeName + ".xml"
        If Not IO.File.Exists(sTypeMultiplierFile) Then
            Dim oConcurrentMultiplier As New ConcurrentDictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
            Dim oMultiplierWorkList As New ConcurrentBag(Of IO.FileInfo)(oImageFiles)
            Dim iConcurrentProcessing As Integer = 0
            Dim iConcurrentProcessingSteps As Integer = oMultiplierWorkList.Count * SuperpixelList.Count

            Dim TaskDelegate As Action(Of Object) = Sub(ByVal oParam As Tuple(Of Integer, IO.FileInfo))
                                                        Using oBitmap As New System.Drawing.Bitmap(oParam.Item2.FullName)
                                                            Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                                                Dim oBounds As New System.Drawing.Rectangle(0, 0, oMatrix.Width, oMatrix.Height)
                                                                Dim iSegments As Integer = 0

                                                                If Not oConcurrentMultiplier.ContainsKey(oParam.Item2.Name) Then
                                                                    oConcurrentMultiplier.TryAdd(oParam.Item2.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                                                                End If
                                                                For Each iSuperpixel In SuperpixelList
                                                                    If Not oConcurrentMultiplier(oParam.Item2.Name).ContainsKey(iSuperpixel) Then
                                                                        oConcurrentMultiplier(oParam.Item2.Name).Add(iSuperpixel, New Dictionary(Of SegmentType, Integer))
                                                                    End If

                                                                    If Not oConcurrentMultiplier(oParam.Item2.Name)(iSuperpixel).ContainsKey(oType) Then
                                                                        Dim oLargeMulResult As New Dictionary(Of Integer, Integer)
                                                                        For Each iMul In oLargeMul
                                                                            Dim oLabels As Matrix(Of Integer) = Nothing
                                                                            Dim fCurrentMul As Single = CSng(iMul) / CSng(MulFactor)
                                                                            Segment(oBounds, oMatrix, oLabels, iSuperpixel, fCurrentMul, True, oType, iSegments)
                                                                            oLargeMulResult.Add(iMul, iSegments)

                                                                            If MatrixNotNothing(oLabels) Then
                                                                                oLabels.Dispose()
                                                                                oLabels = Nothing
                                                                            End If
                                                                        Next

                                                                        Dim bReversed As Boolean = oLargeMulResult.First.Value > oLargeMulResult.Last.Value
                                                                        If bReversed Then
                                                                            Dim iUpperResult As Integer = oLargeMulResult.Last.Key
                                                                            Dim iLowerResult As Integer = iUpperResult
                                                                            For i = oLargeMulResult.Count - 2 To 0 Step -1
                                                                                If oLargeMulResult(oLargeMulResult.Keys(i)) >= iSuperpixel Then
                                                                                    iLowerResult = oLargeMulResult.Keys(i)
                                                                                    Exit For
                                                                                Else
                                                                                    iUpperResult = oLargeMulResult.Keys(i)
                                                                                End If
                                                                            Next

                                                                            For iMul = iUpperResult To iLowerResult Step -(MulStepSmall * MulFactor)
                                                                                Dim oLabels As Matrix(Of Integer) = Nothing

                                                                                Dim fCurrentMul As Single = CSng(iMul) / CSng(MulFactor)
                                                                                Segment(oBounds, oMatrix, oLabels, iSuperpixel, fCurrentMul, True, oType, iSegments)

                                                                                If MatrixNotNothing(oLabels) Then
                                                                                    oLabels.Dispose()
                                                                                    oLabels = Nothing
                                                                                End If

                                                                                If iSegments >= iSuperpixel Then
                                                                                    oConcurrentMultiplier(oParam.Item2.Name)(iSuperpixel).Add(oType, iMul)
                                                                                    Exit For
                                                                                End If
                                                                            Next
                                                                        Else
                                                                            Dim iLowerResult As Integer = oLargeMulResult.First.Key
                                                                            Dim iUpperResult As Integer = iLowerResult
                                                                            For i = 1 To oLargeMulResult.Count - 1
                                                                                If oLargeMulResult(oLargeMulResult.Keys(i)) >= iSuperpixel Then
                                                                                    iUpperResult = oLargeMulResult.Keys(i)
                                                                                    Exit For
                                                                                Else
                                                                                    iLowerResult = oLargeMulResult.Keys(i)
                                                                                End If
                                                                            Next

                                                                            For iMul = iLowerResult To iUpperResult Step (MulStepSmall * MulFactor)
                                                                                Dim oLabels As Matrix(Of Integer) = Nothing

                                                                                Dim fCurrentMul As Single = CSng(iMul) / CSng(MulFactor)
                                                                                Segment(oBounds, oMatrix, oLabels, iSuperpixel, fCurrentMul, True, oType, iSegments)

                                                                                If MatrixNotNothing(oLabels) Then
                                                                                    oLabels.Dispose()
                                                                                    oLabels = Nothing
                                                                                End If

                                                                                If iSegments >= iSuperpixel Then
                                                                                    oConcurrentMultiplier(oParam.Item2.Name)(iSuperpixel).Add(oType, iMul)
                                                                                    Exit For
                                                                                End If
                                                                            Next
                                                                        End If
                                                                    End If

                                                                    Dim iConcurrentProcessingLocal As Integer = System.Threading.Interlocked.Increment(iConcurrentProcessing)
                                                                    Console.WriteLine(GetElapsed(oStartDate) + " Preprocessing " + iConcurrentProcessingLocal.ToString + "/" + iConcurrentProcessingSteps.ToString + ": " + [Enum].GetName(GetType(SegmentType), oType))
                                                                Next
                                                            End Using
                                                        End Using
                                                    End Sub

            If m_SingleProcess.Contains(oType) Then
                For i = 0 To oMultiplierWorkList.Count - 1
                    TaskDelegate.Invoke(New Tuple(Of Integer, IO.FileInfo)(i, oMultiplierWorkList(i)))
                Next
            Else
                ParallelProcess(oMultiplierWorkList.ToList, TaskDelegate, AddressOf UpdateTasks, AddressOf CPUUtilisation, 4, 8)
            End If

            Dim oTypeMultiplier As New Result
            For Each oKeyValue1 In oConcurrentMultiplier
                oTypeMultiplier.Results.Add(oKeyValue1.Key, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                For Each oKeyValue2 In oKeyValue1.Value
                    oTypeMultiplier.Results(oKeyValue1.Key).Add(oKeyValue2.Key, New Dictionary(Of SegmentType, Integer))
                    For Each oKeyValue3 In oKeyValue2.Value
                        If oKeyValue3.Key = oType Then
                            oTypeMultiplier.Results(oKeyValue1.Key)(oKeyValue2.Key).Add(oKeyValue3.Key, oKeyValue3.Value)
                        End If
                    Next
                Next
            Next

            SerializeDataContractFile(sTypeMultiplierFile, oTypeMultiplier, Result.GetKnownTypes, , , False)
            Console.WriteLine(GetElapsed(oStartDate) + " Multiplier " + sTypeName + " Saved")
        End If
    End Sub
    Private Sub ProcessMultipler(ByVal oMultiplierWorkList As List(Of IO.FileInfo), ByVal oLargeMul As List(Of Integer), ByVal oType As SegmentType, ByVal oStartDate As Date, ByVal oSelectedPath As String)
        Dim oMultiplier As New Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
        Dim iProcessing As Integer = 0
        Dim iProcessingSteps As Integer = oMultiplierWorkList.Count * SuperpixelList.Count
        For Each oFileInfo In oMultiplierWorkList
            Using oBitmap As New System.Drawing.Bitmap(oFileInfo.FullName)
                Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                    Dim oBounds As New System.Drawing.Rectangle(0, 0, oMatrix.Width, oMatrix.Height)
                    Dim iSegments As Integer = 0

                    If Not oMultiplier.ContainsKey(oFileInfo.Name) Then
                        oMultiplier.Add(oFileInfo.Name, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
                    End If
                    For Each iSuperpixel In SuperpixelList
                        If Not oMultiplier(oFileInfo.Name).ContainsKey(iSuperpixel) Then
                            oMultiplier(oFileInfo.Name).Add(iSuperpixel, New Dictionary(Of SegmentType, Integer))
                        End If

                        If Not oMultiplier(oFileInfo.Name)(iSuperpixel).ContainsKey(oType) Then
                            Dim iMul As Integer = GetMultiplier(oLargeMul, oBounds, oMatrix, iSuperpixel, oType, iSegments)
                            oMultiplier(oFileInfo.Name)(iSuperpixel).Add(oType, iMul)
                        End If

                        Console.WriteLine(GetElapsed(oStartDate) + " Preprocessing " + iProcessing.ToString + "/" + iProcessingSteps.ToString + ": " + [Enum].GetName(GetType(SegmentType), oType))
                        iProcessing += 1
                    Next
                End Using
            End Using
        Next

        Dim oTypeMultiplier As New Result
        For Each oKeyValue1 In oMultiplier
            oTypeMultiplier.Results.Add(oKeyValue1.Key, New Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
            For Each oKeyValue2 In oKeyValue1.Value
                oTypeMultiplier.Results(oKeyValue1.Key).Add(oKeyValue2.Key, New Dictionary(Of SegmentType, Integer))
                For Each oKeyValue3 In oKeyValue2.Value
                    If oKeyValue3.Key = oType Then
                        oTypeMultiplier.Results(oKeyValue1.Key)(oKeyValue2.Key).Add(oKeyValue3.Key, oKeyValue3.Value)
                    End If
                Next
            Next
        Next

        Dim sTypeName As String = [Enum].GetName(GetType(SegmentType), oType)
        Dim sTypeMultiplierFile As String = oSelectedPath + SaveDirectory + "Multipliers_" + sTypeName + ".xml"
        SerializeDataContractFile(sTypeMultiplierFile, oTypeMultiplier, Result.GetKnownTypes, , , False)
        Console.WriteLine(GetElapsed(oStartDate) + " Multiplier " + sTypeName + " Saved")
    End Sub
    Private Function GetMultiplier(ByVal oLargeMul As List(Of Integer), ByVal oBounds As System.Drawing.Rectangle, ByVal oMatrix As Matrix(Of Byte), ByVal iSuperpixel As Integer, ByVal oType As SegmentType, ByRef iSegments As Integer) As Integer
        ' gets multiplier for type
        Dim iReturnMul As Integer = 0
        Dim oLargeMulResult As New Dictionary(Of Integer, Integer)
        For Each iMul In oLargeMul
            Dim oLabels As Matrix(Of Integer) = Nothing
            Dim fCurrentMul As Single = CSng(iMul) / CSng(MulFactor)
            Segment(oBounds, oMatrix, oLabels, iSuperpixel, fCurrentMul, True, oType, iSegments)
            oLargeMulResult.Add(iMul, iSegments)

            If MatrixNotNothing(oLabels) Then
                oLabels.Dispose()
                oLabels = Nothing
            End If
        Next

        Dim bReversed As Boolean = oLargeMulResult.First.Value > oLargeMulResult.Last.Value
        If bReversed Then
            Dim iUpperResult As Integer = oLargeMulResult.Last.Key
            Dim iLowerResult As Integer = iUpperResult
            For i = oLargeMulResult.Count - 2 To 0 Step -1
                If oLargeMulResult(oLargeMulResult.Keys(i)) >= iSuperpixel Then
                    iLowerResult = oLargeMulResult.Keys(i)
                    Exit For
                Else
                    iUpperResult = oLargeMulResult.Keys(i)
                End If
            Next

            ' exceeds range
            If iUpperResult = oLargeMulResult.First.Key Then
                iReturnMul = iUpperResult
            Else
                For iMul = iUpperResult To iLowerResult Step -(MulStepSmall * MulFactor)
                    Dim oLabels As Matrix(Of Integer) = Nothing

                    Dim fCurrentMul As Single = CSng(iMul) / CSng(MulFactor)
                    Segment(oBounds, oMatrix, oLabels, iSuperpixel, fCurrentMul, True, oType, iSegments)

                    If MatrixNotNothing(oLabels) Then
                        oLabels.Dispose()
                        oLabels = Nothing
                    End If

                    If iSegments >= iSuperpixel Then
                        iReturnMul = iMul
                        Exit For
                    End If
                Next
            End If
        Else
            Dim iLowerResult As Integer = oLargeMulResult.First.Key
            Dim iUpperResult As Integer = iLowerResult
            For i = 1 To oLargeMulResult.Count - 1
                If oLargeMulResult(oLargeMulResult.Keys(i)) >= iSuperpixel Then
                    iUpperResult = oLargeMulResult.Keys(i)
                    Exit For
                Else
                    iLowerResult = oLargeMulResult.Keys(i)
                End If
            Next

            ' exceeds range
            If iLowerResult = oLargeMulResult.Last.Key Then
                iReturnMul = iLowerResult
            Else
                For iMul = iLowerResult To iUpperResult Step (MulStepSmall * MulFactor)
                    Dim oLabels As Matrix(Of Integer) = Nothing

                    Dim fCurrentMul As Single = CSng(iMul) / CSng(MulFactor)
                    Segment(oBounds, oMatrix, oLabels, iSuperpixel, fCurrentMul, True, oType, iSegments)

                    If MatrixNotNothing(oLabels) Then
                        oLabels.Dispose()
                        oLabels = Nothing
                    End If

                    If iSegments >= iSuperpixel Then
                        iReturnMul = iMul
                        Exit For
                    End If
                Next
            End If
        End If
        Return iReturnMul
    End Function
    Private Function Segment(ByVal oBounds As System.Drawing.Rectangle, ByVal oMatrixIn As Matrix(Of Byte), ByRef oLabelsOut As Matrix(Of Integer), ByVal iSuperpixels As Integer, ByVal fMultiplier As Single, ByVal bMerge As Boolean, ByVal oType As SegmentType, ByRef iSegments As Integer) As Integer
        Dim oCroppedBounds = System.Drawing.Rectangle.Intersect(oBounds, New System.Drawing.Rectangle(0, 0, oMatrixIn.Width, oMatrixIn.Height))

        If MatrixNotNothing(oLabelsOut) Then
            oLabelsOut.Dispose()
        End If
        oLabelsOut = New Matrix(Of Integer)(oMatrixIn.Height, oMatrixIn.Width)

        Dim iMatStructSize As Integer = Marshal.SizeOf(GetType(MatStruct))
        Dim oMatStructIn As New MatStruct(oMatrixIn)
        Dim oMatPointerIn As IntPtr = Marshal.AllocCoTaskMem(iMatStructSize)
        Marshal.StructureToPtr(oMatStructIn, oMatPointerIn, False)

        Dim oMatBufferIn As Byte() = oMatrixIn.Bytes
        Dim oMatBufferHandleIn As GCHandle = GCHandle.Alloc(oMatBufferIn, GCHandleType.Pinned)

        Dim oLabelsStructOut As New MatStruct(oLabelsOut)
        Dim oLabelsPointerOut As IntPtr = Marshal.AllocCoTaskMem(iMatStructSize)
        Marshal.StructureToPtr(oLabelsStructOut, oLabelsPointerOut, False)

        Dim oLabelsBufferOut As Byte() = oLabelsOut.Bytes
        Dim oLabelsBufferHandleOut As GCHandle = GCHandle.Alloc(oLabelsBufferOut, GCHandleType.Pinned)

        Dim iDuration As Int32 = 0
        iSegments = segment(oMatPointerIn, oMatBufferHandleIn.AddrOfPinnedObject, oLabelsPointerOut, oLabelsBufferHandleOut.AddrOfPinnedObject, oCroppedBounds.X, oCroppedBounds.Y, oCroppedBounds.Width, oCroppedBounds.Height, iSuperpixels, fMultiplier, bMerge, oType, iDuration)

        Marshal.FreeCoTaskMem(oMatPointerIn)
        oMatBufferHandleIn.Free()

        Marshal.FreeCoTaskMem(oLabelsPointerOut)
        oLabelsBufferHandleOut.Free()

        ClearMemory()

        ' copy back result to mask
        Using oReturnArray As Matrix(Of Integer) = oLabelsStructOut.GetMatrix(Of Integer)(oLabelsBufferOut)
            If MatrixIsNothing(oReturnArray) Then
                oLabelsOut = Nothing
            Else
                oReturnArray.Mat.CopyTo(oLabelsOut)
            End If
        End Using

        Return iDuration
    End Function
    Private Function ConvertLabelsRandom(ByVal oLabels As Matrix(Of Integer)) As Matrix(Of Byte)
        ' converts integer labels to randomly labelled colour matrix
        If MatrixIsNothing(m_LUT(0)) OrElse MatrixIsNothing(m_LUT(1)) OrElse MatrixIsNothing(m_LUT(2)) OrElse MatrixIsNothing(m_LUT(3)) Then
            For i = 0 To 3
                m_LUT(i) = New Matrix(Of Byte)(256, 1, 3)
                Using oBLUT As New Matrix(Of Byte)(256, 1)
                    Using oGLUT As New Matrix(Of Byte)(256, 1)
                        Using oRLUT As New Matrix(Of Byte)(256, 1)
                            For y = 0 To 255
                                oBLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                                oGLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                                oRLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                            Next

                            CvInvoke.Merge(New Util.VectorOfMat({oBLUT.Mat, oGLUT.Mat, oRLUT.Mat}), m_LUT(i))
                        End Using
                    End Using
                End Using
            Next
        End If

        ' convert to colour matrices
        Dim oLabelsOut As Matrix(Of Byte) = Nothing
        If MatrixNotNothing(oLabels) Then
            oLabelsOut = New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
            Using oLabels0 As New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
                Using oLabels1 As New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
                    Using oLabels2 As New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
                        Using oLabels3 As New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
                            Using oTemp As Matrix(Of Integer) = oLabels.CopyBlank
                                oTemp.SetValue(&HFF000000)
                                CvInvoke.BitwiseAnd(oLabels, oTemp, oTemp)
                                oTemp._Mul(1.0 / CDbl(&H1000000))
                                CvInvoke.CvtColor(oTemp.Convert(Of Byte), oLabels3, CvEnum.ColorConversion.Gray2Bgr)
                                CvInvoke.LUT(oLabels3, m_LUT(3), oLabels3)

                                oTemp.SetValue(&HFF0000)
                                CvInvoke.BitwiseAnd(oLabels, oTemp, oTemp)
                                oTemp._Mul(1.0 / CDbl(&H10000))
                                CvInvoke.CvtColor(oTemp.Convert(Of Byte), oLabels2, CvEnum.ColorConversion.Gray2Bgr)
                                CvInvoke.LUT(oLabels2, m_LUT(2), oLabels2)

                                oTemp.SetValue(&HFF00)
                                CvInvoke.BitwiseAnd(oLabels, oTemp, oTemp)
                                oTemp._Mul(1.0 / CDbl(&H100))
                                CvInvoke.CvtColor(oTemp.Convert(Of Byte), oLabels1, CvEnum.ColorConversion.Gray2Bgr)
                                CvInvoke.LUT(oLabels1, m_LUT(1), oLabels1)

                                oTemp.SetValue(&HFF)
                                CvInvoke.BitwiseAnd(oLabels, oTemp, oTemp)
                                CvInvoke.CvtColor(oTemp.Convert(Of Byte), oLabels0, CvEnum.ColorConversion.Gray2Bgr)
                                CvInvoke.LUT(oLabels0, m_LUT(0), oLabels0)
                            End Using
                            CvInvoke.BitwiseXor(oLabels0, oLabels1, oLabelsOut)
                            CvInvoke.BitwiseXor(oLabels2, oLabelsOut, oLabelsOut)
                            CvInvoke.BitwiseXor(oLabels3, oLabelsOut, oLabelsOut)
                            oLabelsOut.SubR(255).CopyTo(oLabelsOut)
                        End Using
                    End Using
                End Using
            End Using
        End If

        Using oNoiseMask As New Matrix(Of Byte)(oLabels.Size)
            Using oNoiseCompare As New Matrix(Of Integer)(oLabels.Size)
                oNoiseCompare.SetValue(0)
                CvInvoke.Compare(oLabels, oNoiseCompare, oNoiseMask, CvEnum.CmpType.LessThan)
                oLabelsOut.SetValue(New [Structure].MCvScalar(0, 0, 0), oNoiseMask)
            End Using
        End Using
        Return oLabelsOut
    End Function
    Private Function ConvertLabelsMean(ByVal oLabels As Matrix(Of Integer), ByVal oImage As Matrix(Of Byte)) As Matrix(Of Byte)
        ' converts integer labels to mean colour of the image areas
        Dim oLabelsList As New List(Of Integer)
        For y = 0 To oLabels.Height - 1
            For x = 0 To oLabels.Width - 1
                oLabelsList.Add(oLabels(y, x))
            Next
        Next
        oLabelsList = oLabelsList.Distinct.ToList

        Dim oLabelsDict As New Dictionary(Of Integer, Tuple(Of List(Of Integer), List(Of Integer), List(Of Integer)))
        For i = 0 To oLabelsList.Count - 1
            oLabelsDict.Add(oLabelsList(i), New Tuple(Of List(Of Integer), List(Of Integer), List(Of Integer))(New List(Of Integer), New List(Of Integer), New List(Of Integer)))
        Next

        Using oVector As New Util.VectorOfMat
            CvInvoke.Split(oImage, oVector)
            Using oBMatrix As New Matrix(Of Byte)(oImage.Rows, oImage.Cols, oVector(0).DataPointer)
                Using oGMatrix As New Matrix(Of Byte)(oImage.Rows, oImage.Cols, oVector(1).DataPointer)
                    Using oRMatrix As New Matrix(Of Byte)(oImage.Rows, oImage.Cols, oVector(2).DataPointer)
                        For y = 0 To oLabels.Height - 1
                            For x = 0 To oLabels.Width - 1
                                Dim iLabel As Integer = oLabels(y, x)
                                oLabelsDict(iLabel).Item1.Add(oBMatrix(y, x))
                                oLabelsDict(iLabel).Item2.Add(oGMatrix(y, x))
                                oLabelsDict(iLabel).Item3.Add(oRMatrix(y, x))
                            Next
                        Next
                    End Using
                End Using
            End Using
            oVector(0).Dispose()
            oVector(1).Dispose()
            oVector(2).Dispose()
        End Using

        Dim oLabelsMeanDict As New Dictionary(Of Integer, Tuple(Of Byte, Byte, Byte))
        For i = 0 To oLabelsList.Count - 1
            oLabelsMeanDict.Add(oLabelsList(i), New Tuple(Of Byte, Byte, Byte)(CByte(oLabelsDict(oLabelsList(i)).Item1.Average), CByte(oLabelsDict(oLabelsList(i)).Item2.Average), CByte(oLabelsDict(oLabelsList(i)).Item3.Average)))
        Next

        Dim oLabelsOut As New Matrix(Of Byte)(oImage.Rows, oImage.Cols, 3)
        Using oBMatrix As New Matrix(Of Byte)(oImage.Rows, oImage.Cols)
            Using oGMatrix As New Matrix(Of Byte)(oImage.Rows, oImage.Cols)
                Using oRMatrix As New Matrix(Of Byte)(oImage.Rows, oImage.Cols)
                    For y = 0 To oLabels.Height - 1
                        For x = 0 To oLabels.Width - 1
                            Dim iLabel As Integer = oLabels(y, x)
                            oBMatrix(y, x) = oLabelsMeanDict(iLabel).Item1
                            oGMatrix(y, x) = oLabelsMeanDict(iLabel).Item2
                            oRMatrix(y, x) = oLabelsMeanDict(iLabel).Item3
                        Next
                    Next
                    CvInvoke.Merge(New Util.VectorOfMat({oBMatrix.Mat, oGMatrix.Mat, oRMatrix.Mat}), oLabelsOut)
                End Using
            End Using
        End Using

        Return oLabelsOut
    End Function
    Private Function ConvertLabelsOutline(ByVal oLabels As Matrix(Of Integer), ByVal oImage As Matrix(Of Byte)) As Matrix(Of Byte)
        ' draws the outline of the clusters in black on the image
        Dim oLabelsOut As Matrix(Of Byte) = oImage.Clone

        Dim oLabelsList As New List(Of Integer)
        For y = 0 To oLabels.Height - 1
            For x = 0 To oLabels.Width - 1
                oLabelsList.Add(oLabels(y, x))
            Next
        Next
        oLabelsList = oLabelsList.Distinct.ToList

        Using oMask As New Matrix(Of Byte)(oLabels.Size)
            Using oCompare As Matrix(Of Integer) = oLabels.CopyBlank
                For Each iLabel In oLabelsList
                    oCompare.SetValue(iLabel)
                    CvInvoke.Compare(oLabels, oCompare, oMask, CvEnum.CmpType.Equal)
                    Using oContours As New Util.VectorOfVectorOfPoint
                        Using oHierarchy As New Mat
                            CvInvoke.FindContours(oMask, oContours, oHierarchy, CvEnum.RetrType.External, CvEnum.ChainApproxMethod.ChainApproxSimple)
                            CvInvoke.DrawContours(oLabelsOut, oContours, -1, New [Structure].MCvScalar(0, 0, 0), 1)
                        End Using
                    End Using
                Next
            End Using
        End Using

        Return oLabelsOut
    End Function
    Private Function ConvertLabelsOutlineOnly(ByVal oLabels As Matrix(Of Integer)) As Matrix(Of Byte)
        ' draws the outline of the clusters in black on the image
        Dim oLabelsOut As New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
        oLabelsOut.SetZero()

        Dim oLabelsList As New List(Of Integer)
        For y = 0 To oLabels.Height - 1
            For x = 0 To oLabels.Width - 1
                oLabelsList.Add(oLabels(y, x))
            Next
        Next
        oLabelsList = oLabelsList.Distinct.ToList

        Using oMask As New Matrix(Of Byte)(oLabels.Size)
            Using oCompare As Matrix(Of Integer) = oLabels.CopyBlank
                For Each iLabel In oLabelsList
                    oCompare.SetValue(iLabel)
                    CvInvoke.Compare(oLabels, oCompare, oMask, CvEnum.CmpType.Equal)
                    Using oContours As New Util.VectorOfVectorOfPoint
                        Using oHierarchy As New Mat
                            CvInvoke.FindContours(oMask, oContours, oHierarchy, CvEnum.RetrType.External, CvEnum.ChainApproxMethod.ChainApproxSimple)
                            CvInvoke.DrawContours(oLabelsOut, oContours, -1, New [Structure].MCvScalar(255, 255, 255), 1)
                        End Using
                    End Using
                Next
            End Using
        End Using

        Return oLabelsOut
    End Function
    Private Function ConvertLabels(ByVal oLabels As Matrix(Of Integer), ByVal oImage As Matrix(Of Byte), ByVal oType As LabelType) As Matrix(Of Byte)
        Dim oLabelsOut As Matrix(Of Byte) = Nothing
        Select Case oType
            Case LabelType.Mean
                oLabelsOut = ConvertLabelsMean(oLabels, oImage)
            Case LabelType.Random
                oLabelsOut = ConvertLabelsRandom(oLabels)
            Case LabelType.Outline
                oLabelsOut = ConvertLabelsOutline(oLabels, oImage)
            Case LabelType.OutlineOnly
                oLabelsOut = ConvertLabelsOutlineOnly(oLabels)
        End Select
        Return oLabelsOut
    End Function
    Private Function ConvertUShortLabels(ByVal oUShortLabels As Matrix(Of UShort)) As Matrix(Of Byte)
        ' converts ushort labels into color segmentation images
        Dim iHeight As Integer = oUShortLabels.Height
        Dim iWidth As Integer = oUShortLabels.Width
        Dim oSegmentationMatrix As Matrix(Of Byte) = New Matrix(Of Byte)(iHeight, iWidth, 3)
        oSegmentationMatrix.SetZero()
        Using oMatrixByteB As New Matrix(Of Byte)(iHeight, iWidth)
            Using oMatrixByteG As New Matrix(Of Byte)(iHeight, iWidth)
                Using oMatrixByteR As New Matrix(Of Byte)(iHeight, iWidth)
                    For y = 0 To iHeight - 1
                        For x = 0 To iWidth - 1
                            Dim iValue As UShort = oUShortLabels(y, x)
                            Dim bB As Byte = &B0000_0011
                            Dim bG As Byte = &B0000_0011
                            Dim bR As Byte = &B0000_0011
                            If (iValue And &B0000_0000_0000_0001) <> 0 Then
                                bB = bB Or &B1000_0000
                            End If
                            If (iValue And &B0000_0000_0000_0010) <> 0 Then
                                bG = bG Or &B1000_0000
                            End If
                            If (iValue And &B0000_0000_0000_0100) <> 0 Then
                                bR = bR Or &B1000_0000
                            End If
                            If (iValue And &B0000_0000_0000_1000) <> 0 Then
                                bB = bB Or &B0100_0000
                            End If
                            If (iValue And &B0000_0000_0001_0000) <> 0 Then
                                bG = bG Or &B0100_0000
                            End If
                            If (iValue And &B0000_0000_0010_0000) <> 0 Then
                                bR = bR Or &B0100_0000
                            End If
                            If (iValue And &B0000_0000_0100_0000) <> 0 Then
                                bB = bB Or &B0010_0000
                            End If
                            If (iValue And &B0000_0000_1000_0000) <> 0 Then
                                bG = bG Or &B0010_0000
                            End If
                            If (iValue And &B0000_0001_0000_0000) <> 0 Then
                                bR = bR Or &B0010_0000
                            End If
                            If (iValue And &B0000_0010_0000_0000) <> 0 Then
                                bB = bB Or &B0001_0000
                            End If
                            If (iValue And &B0000_0100_0000_0000) <> 0 Then
                                bG = bG Or &B0001_0000
                            End If
                            If (iValue And &B0000_1000_0000_0000) <> 0 Then
                                bR = bR Or &B0001_0000
                            End If
                            If (iValue And &B0001_0000_0000_0000) <> 0 Then
                                bB = bB Or &B0000_1000
                            End If
                            If (iValue And &B0010_0000_0000_0000) <> 0 Then
                                bG = bG Or &B0000_1000
                            End If
                            If (iValue And &B0100_0000_0000_0000) <> 0 Then
                                bR = bR Or &B0000_1000
                            End If
                            If (iValue And &B1000_0000_0000_0000) <> 0 Then
                                bB = bB Or &B0000_0100
                            End If
                            oMatrixByteB(y, x) = bB
                            oMatrixByteG(y, x) = bG
                            oMatrixByteR(y, x) = bR
                        Next
                    Next

                    CvInvoke.Merge(New Util.VectorOfMat({oMatrixByteB.Mat, oMatrixByteG.Mat, oMatrixByteR.Mat}), oSegmentationMatrix)
                End Using
            End Using
        End Using
        Return oSegmentationMatrix
    End Function
    Private Function UnconvertUShortLabels(ByVal oSegmentationMatrix As Matrix(Of Byte)) As Matrix(Of UShort)
        ' converts color segmentation images into ushort labels
        Dim iHeight As Integer = oSegmentationMatrix.Height
        Dim iWidth As Integer = oSegmentationMatrix.Width
        Dim oUShortLabels As Matrix(Of UShort) = New Matrix(Of UShort)(iHeight, iWidth)
        oUShortLabels.SetZero()
        Using oVectorMat As New Util.VectorOfMat
            CvInvoke.Split(oSegmentationMatrix, oVectorMat)
            Using oMatrixByteB As New Matrix(Of Byte)(iHeight, iWidth, oVectorMat(0).DataPointer)
                Using oMatrixByteG As New Matrix(Of Byte)(iHeight, iWidth, oVectorMat(1).DataPointer)
                    Using oMatrixByteR As New Matrix(Of Byte)(iHeight, iWidth, oVectorMat(2).DataPointer)
                        For y = 0 To iHeight - 1
                            For x = 0 To iWidth - 1
                                Dim bB As Byte = oMatrixByteB(y, x) And &B1111_1100
                                Dim bG As Byte = oMatrixByteG(y, x) And &B1111_1100
                                Dim bR As Byte = oMatrixByteR(y, x) And &B1111_1100
                                Dim iValue As UShort = &B0000_0000_0000_0000
                                If (bB And &B1000_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_0000_0001
                                End If
                                If (bG And &B1000_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_0000_0010
                                End If
                                If (bR And &B1000_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_0000_0100
                                End If
                                If (bB And &B0100_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_0000_1000
                                End If
                                If (bG And &B0100_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_0001_0000
                                End If
                                If (bR And &B0100_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_0010_0000
                                End If
                                If (bB And &B0010_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_0100_0000
                                End If
                                If (bG And &B0010_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0000_1000_0000
                                End If
                                If (bR And &B0010_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0001_0000_0000
                                End If
                                If (bB And &B0001_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0010_0000_0000
                                End If
                                If (bG And &B0001_0000) <> 0 Then
                                    iValue = iValue Or &B0000_0100_0000_0000
                                End If
                                If (bR And &B0001_0000) <> 0 Then
                                    iValue = iValue Or &B0000_1000_0000_0000
                                End If
                                If (bB And &B0000_1000) <> 0 Then
                                    iValue = iValue Or &B0001_0000_0000_0000
                                End If
                                If (bG And &B0000_1000) <> 0 Then
                                    iValue = iValue Or &B0010_0000_0000_0000
                                End If
                                If (bR And &B0000_1000) <> 0 Then
                                    iValue = iValue Or &B0100_0000_0000_0000
                                End If
                                If (bB And &B0000_0100) <> 0 Then
                                    iValue = iValue Or &B1000_0000_0000_0000
                                End If
                                oUShortLabels(y, x) = iValue
                            Next
                        Next
                    End Using
                End Using
            End Using
        End Using

        Return oUShortLabels
    End Function
    Private Function GetElapsed(ByVal oStartDate As Date) As String
        Return CInt((Date.Now - oStartDate).TotalSeconds).ToString.PadLeft(5, "0") + "s"
    End Function
    Private Function BitmapToMatrix(ByVal oBitmap As System.Drawing.Bitmap) As Matrix(Of Byte)
        ' convert bitmap to matrix
        If IsNothing(oBitmap) Then
            Return Nothing
        Else
            Dim oReturnMatrix As Matrix(Of Byte) = Nothing
            Dim oRectangle As New System.Drawing.Rectangle(0, 0, oBitmap.Width, oBitmap.Height)
            Dim oBitmapData As System.Drawing.Imaging.BitmapData = oBitmap.LockBits(oRectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly, oBitmap.PixelFormat)

            Select Case oBitmap.PixelFormat
                Case System.Drawing.Imaging.PixelFormat.Format8bppIndexed
                    Using oMat As New Mat(oBitmap.Height, oBitmap.Width, CvEnum.DepthType.Cv8U, 1, oBitmapData.Scan0, oBitmapData.Stride)
                        oReturnMatrix = New Matrix(Of Byte)(oBitmap.Height, oBitmap.Width, 1)
                        oMat.CopyTo(oReturnMatrix)
                    End Using
                Case System.Drawing.Imaging.PixelFormat.Format24bppRgb
                    Using oMat As New Mat(oBitmap.Height, oBitmap.Width, CvEnum.DepthType.Cv8U, 3, oBitmapData.Scan0, oBitmapData.Stride)
                        oReturnMatrix = New Matrix(Of Byte)(oBitmap.Height, oBitmap.Width, 3)
                        oMat.CopyTo(oReturnMatrix)
                    End Using
                Case System.Drawing.Imaging.PixelFormat.Format32bppArgb
                    Using oMat As New Mat(oBitmap.Height, oBitmap.Width, CvEnum.DepthType.Cv8U, 4, oBitmapData.Scan0, oBitmapData.Stride)
                        oReturnMatrix = New Matrix(Of Byte)(oBitmap.Height, oBitmap.Width, 3)
                        CvInvoke.CvtColor(oMat, oReturnMatrix, CvEnum.ColorConversion.Bgra2Bgr)
                    End Using
                Case Else
                    Return Nothing
            End Select

            oBitmap.UnlockBits(oBitmapData)

            Return oReturnMatrix
        End If
    End Function
    Private Function MatrixIsNothing(Of T As Structure)(ByVal oMatrix As Matrix(Of T)) As Boolean
        ' checks if the matrix is nothing or disposed
        Return IsNothing(oMatrix) OrElse oMatrix.Ptr.Equals(IntPtr.Zero)
    End Function
    Private Function MatrixNotNothing(Of T As Structure)(ByVal oMatrix As Matrix(Of T)) As Boolean
        ' checks if the matrix is not nothing and not disposed
        Return (Not IsNothing(oMatrix)) AndAlso (Not oMatrix.Ptr.Equals(IntPtr.Zero))
    End Function
    Private Sub SaveMatrix(ByVal sFileName As String, ByVal oMatrix As Matrix(Of Byte), Optional DPI As Single = Resolution096)
        ' save matrix to TIFF file
        If Not IsNothing(oMatrix) Then
            Dim oBitmapSource As BitmapSource = MatrixToBitmapSource(oMatrix, DPI)
            Try
                Using oFileStream As New IO.FileStream(sFileName, IO.FileMode.Create)
                    Dim oTiffBitmapEncoder As New TiffBitmapEncoder()
                    oTiffBitmapEncoder.Compression = TiffCompressOption.Zip
                    oTiffBitmapEncoder.Frames.Add(BitmapFrame.Create(oBitmapSource))
                    oTiffBitmapEncoder.Save(oFileStream)
                    oFileStream.Flush()
                End Using
            Catch ex As IO.IOException
            End Try
        End If
    End Sub
    Private Function MatrixToBitmapSource(ByVal oMatrix As Matrix(Of Byte), Optional DPI As Single = Resolution096) As BitmapSource
        ' converts matrix data to an array which takes into account the bitmap stride
        If IsNothing(oMatrix) Then
            Return Nothing
        Else
            Using oColourMatrix As New Matrix(Of Byte)(oMatrix.Height, oMatrix.Width, 4)
                Select Case oMatrix.NumberOfChannels
                    Case 4
                        oMatrix.CopyTo(oColourMatrix)
                    Case 3
                        CvInvoke.CvtColor(oMatrix, oColourMatrix, CvEnum.ColorConversion.Bgr2Bgra)
                    Case 2
                        Using oMonoMatrix As New Matrix(Of Byte)(oMatrix.Height, oMatrix.Width)
                            Using oVector As New Util.VectorOfMat
                                CvInvoke.Split(oMatrix, oVector)
                                CvInvoke.AddWeighted(oVector(0), 0.5, oVector(1), 0.5, 0.0, oMonoMatrix)
                            End Using
                            CvInvoke.CvtColor(oMonoMatrix, oColourMatrix, CvEnum.ColorConversion.Gray2Bgra)
                        End Using
                    Case 1
                        CvInvoke.CvtColor(oMatrix, oColourMatrix, CvEnum.ColorConversion.Gray2Bgra)
                    Case Else
                        Throw New ArgumentException("Converter:MatrixToBitmapSource: Number of channels must be 1, 3, or 4")
                End Select

                Dim width As Integer = oColourMatrix.Cols
                Dim height As Integer = oColourMatrix.Rows
                Dim iBytesPerPixel As Integer = 4
                Dim iStride As Integer = width * iBytesPerPixel

                Dim oWriteableBitmap As New WriteableBitmap(width, height, DPI, DPI, PixelFormats.Bgra32, Nothing)
                oWriteableBitmap.WritePixels(New Int32Rect(0, 0, width, height), oColourMatrix.Bytes, iStride, 0, 0)
                Return oWriteableBitmap
            End Using
        End If
    End Function
    Private Sub ClearMemory()
        ' clear up memory
        Runtime.GCSettings.LargeObjectHeapCompactionMode = Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
        GC.Collect(2, GCCollectionMode.Forced, True, True)
        GC.WaitForPendingFinalizers()
    End Sub
    Private Sub ParallelProcess(Of T)(ByVal oInput As List(Of T), ByVal TaskDelegate As Action(Of Object), ByVal oUpdateTaskEvent As EventHandler(Of Tuple(Of Integer, Integer)), Optional oCPUFunc As Func(Of Tuple(Of Integer, Integer, Boolean)) = Nothing, Optional ByVal iStartTasks As Integer = 6, Optional ByVal iMaxTasks As Integer = 8, Optional ByVal bSTA As Boolean = False)
        ' runs a task to process a collection in parallel
        ' fCPUUtilisation is a target CPU utilisation from 0-100, fCPUTolerance is a +/- range beyond which adjustment of tasks occurs
        Const DefaultCPUUtilisation As Integer = 50
        Const DefaultCPUTolerance As Integer = 50
        Const CPUAdjustment As Single = 0.2

        Dim iCurrentProcess As Integer = -1
        Dim iEndProcess As Integer = oInput.Count
        Dim iActiveTasks As Integer = iStartTasks
        oUpdateTaskEvent.Invoke(Nothing, New Tuple(Of Integer, Integer)(iActiveTasks, 0))

        Dim oProcessDict As New ConcurrentDictionary(Of Integer, T)(From iIndex In Enumerable.Range(0, iEndProcess) Select New KeyValuePair(Of Integer, T)(iIndex, oInput(iIndex)))
        Dim oTaskList As New List(Of Task)

        Dim oAction As Action(Of Object) = Sub(ByVal iTaskNumber As Integer)
                                               Do
                                                   Dim iCurrent As Integer = System.Threading.Interlocked.Increment(iCurrentProcess)
                                                   If iCurrent >= iEndProcess Then
                                                       Exit Do
                                                   Else
                                                       TaskDelegate.Invoke(New Tuple(Of Integer, T)(iTaskNumber, oProcessDict(iCurrent)))
                                                   End If

                                                   If iTaskNumber >= iActiveTasks Then
                                                       Exit Do
                                                   End If
                                               Loop
                                           End Sub

        For i = 0 To iActiveTasks - 1
            If bSTA Then
                oTaskList.Add(StartSTATask(oAction, i))
            Else
                oTaskList.Add(New Task(oAction, i))
                oTaskList(i).Start()
            End If
        Next

        Dim oCPUUtilisation As New SingleSmoothing(20)
        Dim oCPUTokenSource As New System.Threading.CancellationTokenSource()
        Dim oCPUCancellationToken As System.Threading.CancellationToken = oCPUTokenSource.Token

        Dim currentProcess As Process = Process.GetCurrentProcess()
        Dim sProcessName As String = currentProcess.ProcessName
        Dim fProcessorCount As Single = CSng(Environment.ProcessorCount)

        Dim oCPUMonitorAction As Action = Sub()
                                              Dim oCPUCounter As New PerformanceCounter("Process", "% Processor Time", sProcessName)
                                              oCPUCounter.NextValue()

                                              Do
                                                  If oCPUCancellationToken.IsCancellationRequested Then
                                                      ' clean up
                                                      oCPUUtilisation.Clear()
                                                      Exit Do
                                                  End If

                                                  System.Threading.Thread.Sleep(1000)

                                                  oCPUUtilisation.Enqueue(oCPUCounter.NextValue / fProcessorCount)

                                                  Dim fCurrentUtilisation As Single = oCPUUtilisation.GetSmoothedQueueResult(0.3)

                                                  If Not oCPUCancellationToken.IsCancellationRequested Then
                                                      Dim iCPUUtilisation As Integer = DefaultCPUUtilisation
                                                      Dim iCPUTolerance As Integer = DefaultCPUTolerance
                                                      Dim bResetCPU As Boolean = False
                                                      If Not IsNothing(oCPUFunc) Then
                                                          Dim oCurrentUtilisation As Tuple(Of Integer, Integer, Boolean) = oCPUFunc.Invoke
                                                          iCPUUtilisation = oCurrentUtilisation.Item1
                                                          iCPUTolerance = oCurrentUtilisation.Item2
                                                          bResetCPU = oCurrentUtilisation.Item3
                                                      End If

                                                      Dim fLowerBounds As Single = Math.Min(Math.Max(iCPUUtilisation - iCPUTolerance, 20), 80)
                                                      Dim fUpperBounds As Single = Math.Min(Math.Max(iCPUUtilisation + iCPUTolerance, 40), 100)

                                                      Dim iRunningTasks As Integer = Aggregate oTask In oTaskList Into Count(oTask.Status = System.Threading.Tasks.TaskStatus.Running)
                                                      If bResetCPU Then
                                                          iActiveTasks = 1
                                                      ElseIf iActiveTasks = iRunningTasks AndAlso fCurrentUtilisation < fLowerBounds AndAlso iActiveTasks < iMaxTasks Then
                                                          ' first try to replace existing completed, faulted, or cancelled tasks
                                                          Dim bAdded As Boolean = False
                                                          For i = 0 To oTaskList.Count - 1
                                                              If oTaskList(i).Status = TaskStatus.RanToCompletion OrElse oTaskList(i).Status = TaskStatus.Faulted OrElse oTaskList(i).Status = TaskStatus.Canceled Then
                                                                  If bSTA Then
                                                                      oTaskList(i) = StartSTATask(oAction, i)
                                                                  Else
                                                                      oTaskList(i) = New Task(oAction, i)
                                                                      oTaskList(i).Start()
                                                                  End If
                                                                  bAdded = True
                                                                  iActiveTasks = Math.Max(iActiveTasks, i + 1)
                                                                  oUpdateTaskEvent.Invoke(Nothing, New Tuple(Of Integer, Integer)(iActiveTasks, fCurrentUtilisation))

                                                                  Exit For
                                                              End If
                                                          Next

                                                          ' add task if not already done
                                                          If Not bAdded Then
                                                              iActiveTasks += 1
                                                              oUpdateTaskEvent.Invoke(Nothing, New Tuple(Of Integer, Integer)(iActiveTasks, fCurrentUtilisation))

                                                              If bSTA Then
                                                                  oTaskList.Add(StartSTATask(oAction, oTaskList.Count))
                                                              Else
                                                                  oTaskList.Add(New Task(oAction, oTaskList.Count))
                                                                  oTaskList(oTaskList.Count - 1).Start()
                                                              End If
                                                          End If
                                                      ElseIf iActiveTasks = iRunningTasks AndAlso fCurrentUtilisation > fUpperBounds AndAlso iActiveTasks > 1 Then
                                                          ' reduce tasks
                                                          Dim iAdjustment = Math.Max((iActiveTasks - 1) * CPUAdjustment, 1)
                                                          iActiveTasks -= iAdjustment
                                                          oUpdateTaskEvent.Invoke(Nothing, New Tuple(Of Integer, Integer)(iActiveTasks, fCurrentUtilisation))
                                                      Else
                                                          oUpdateTaskEvent.Invoke(Nothing, New Tuple(Of Integer, Integer)(iActiveTasks, fCurrentUtilisation))
                                                      End If
                                                  End If
                                              Loop
                                          End Sub

        ' start monitoring on a new thread
        Dim oCPUThread As New System.Threading.Thread(Sub()
                                                          oCPUMonitorAction()
                                                      End Sub)
        oCPUThread.Priority = System.Threading.ThreadPriority.Normal
        oCPUThread.Start()

        Task.WaitAll(oTaskList.ToArray)
        oCPUTokenSource.Cancel()
        oTaskList.Clear()
    End Sub
    Private Function StartSTATask(ByVal oAction As Action(Of Object), state As Object) As Task
        Dim oTCS As New TaskCompletionSource(Of Object)()
        Dim oThread = New System.Threading.Thread(Sub()
                                                      Try
                                                          oAction.Invoke(state)
                                                          oTCS.SetResult(Nothing)
                                                      Catch e As Exception
                                                          oTCS.SetException(e)
                                                      End Try
                                                  End Sub)
        oThread.SetApartmentState(System.Threading.ApartmentState.STA)
        oThread.Start()
        Return oTCS.Task
    End Function
    Private Function CPUUtilisation() As Tuple(Of Integer, Integer, Boolean)
        Return New Tuple(Of Integer, Integer, Boolean)(CPUUtilisationMedium, CPUTolerance, False)
    End Function
    Private Sub UpdateTasks(ByVal sender As Object, ByVal oMessage As Tuple(Of Integer, Integer))
        ' update UI from event
    End Sub
    Private Sub ProtectedRunTasks(ByVal oActions As List(Of Tuple(Of Action(Of Object), Object)))
        ' protects tasks from memory exceptions by clearing memory if an error occurs
        Dim oActionDictionary As New Dictionary(Of Guid, Tuple(Of Action(Of Object), Object))
        For Each oAction As Tuple(Of Action(Of Object), Object) In oActions
            oActionDictionary.Add(Guid.NewGuid, oAction)
        Next

        Do Until oActionDictionary.Count = 0
            ' create task dictionary from actions
            Dim oTaskDictionary As New ConcurrentDictionary(Of Guid, Task)
            For Each oGUID As Guid In oActionDictionary.Keys
                oTaskDictionary.TryAdd(oGUID, New Task(oActionDictionary(oGUID).Item1, oActionDictionary(oGUID).Item2))
            Next

            Try
                For Each oTask In oTaskDictionary.Values
                    oTask.Start()
                Next
                Task.WaitAll(oTaskDictionary.Values.ToArray)
            Catch ae As AggregateException
                Throw New ArgumentException(ae.Message)
            End Try

            For Each oGUID In oTaskDictionary.Keys
                If oTaskDictionary(oGUID).IsCompleted Then
                    oActionDictionary.Remove(oGUID)
                ElseIf oTaskDictionary(oGUID).IsFaulted Then
                    For Each ex In oTaskDictionary(oGUID).Exception.Flatten.InnerExceptions
                        If TypeOf ex Is OutOfMemoryException Then
                            ClearMemory()
                        Else
                            Throw ex
                        End If
                    Next
                End If
            Next

            oTaskDictionary.Clear()
        Loop
    End Sub
    <StructLayout(LayoutKind.Sequential, Pack:=1)> Public Structure MatStruct
        Dim Width As Int32 ''4 bytes
        Dim Height As Int32 ''4 bytes
        Dim Channels As Int32 ''4 bytes
        Dim Length As Int32 ''4 bytes
        Dim BaseType As Int32 ''4 bytes

        Sub New(ByVal oMatrixObject As Object)
            If IsNothing(oMatrixObject) Then
                Width = 0
                Height = 0
                Channels = 0
                Length = 0
                BaseType = 0
            Else
                Select Case oMatrixObject.GetType
                    Case GetType(Matrix(Of Byte))
                        Dim oMatrix As Matrix(Of Byte) = oMatrixObject
                        Width = Convert.ToInt32(oMatrix.Width)
                        Height = Convert.ToInt32(oMatrix.Height)
                        Channels = Convert.ToInt32(oMatrix.NumberOfChannels)
                        Length = Convert.ToInt32(oMatrix.Bytes.Length)
                        BaseType = 0
                    Case GetType(Matrix(Of UShort))
                        Dim oMatrix As Matrix(Of UShort) = oMatrixObject
                        Width = Convert.ToInt32(oMatrix.Width)
                        Height = Convert.ToInt32(oMatrix.Height)
                        Channels = Convert.ToInt32(oMatrix.NumberOfChannels)
                        Length = Convert.ToInt32(oMatrix.Bytes.Length)
                        BaseType = 2
                    Case GetType(Matrix(Of Short))
                        Dim oMatrix As Matrix(Of Short) = oMatrixObject
                        Width = Convert.ToInt32(oMatrix.Width)
                        Height = Convert.ToInt32(oMatrix.Height)
                        Channels = Convert.ToInt32(oMatrix.NumberOfChannels)
                        Length = Convert.ToInt32(oMatrix.Bytes.Length)
                        BaseType = 3
                    Case GetType(Matrix(Of Integer))
                        Dim oMatrix As Matrix(Of Integer) = oMatrixObject
                        Width = Convert.ToInt32(oMatrix.Width)
                        Height = Convert.ToInt32(oMatrix.Height)
                        Channels = Convert.ToInt32(oMatrix.NumberOfChannels)
                        Length = Convert.ToInt32(oMatrix.Bytes.Length)
                        BaseType = 4
                    Case GetType(Matrix(Of Single))
                        Dim oMatrix As Matrix(Of Single) = oMatrixObject
                        Width = Convert.ToInt32(oMatrix.Width)
                        Height = Convert.ToInt32(oMatrix.Height)
                        Channels = Convert.ToInt32(oMatrix.NumberOfChannels)
                        Length = Convert.ToInt32(oMatrix.Bytes.Length)
                        BaseType = 5
                    Case GetType(Matrix(Of Double))
                        Dim oMatrix As Matrix(Of Double) = oMatrixObject
                        Width = Convert.ToInt32(oMatrix.Width)
                        Height = Convert.ToInt32(oMatrix.Height)
                        Channels = Convert.ToInt32(oMatrix.NumberOfChannels)
                        Length = Convert.ToInt32(oMatrix.Bytes.Length)
                        BaseType = 6
                End Select
            End If
        End Sub
        Function GetMatrix(Of T As Structure)(ByVal Bytes As Byte()) As Matrix(Of T)
            If Bytes.Count = 0 OrElse Width = 0 OrElse Height = 0 OrElse Channels = 0 Then
                Return Nothing
            Else
                Return ArrayToMatrix(Of T)(Bytes, Width, Height, Channels)
            End If
        End Function
        Private Function ArrayToMatrix(Of T As Structure)(ByVal oArray As Byte(), ByVal width As Integer, ByVal height As Integer, ByVal channels As Integer) As Matrix(Of T)
            ' convert a two dimensional array to a one dimensional array equivalent
            Dim oMatrix As New Matrix(Of T)(height, width, channels)
            oMatrix.Bytes = oArray
            Return oMatrix
        End Function
    End Structure
    Enum SegmentType As Int32
        ScanSegment = 0
        FastSuperpixel = 1
        SEEDS = 2
        LSC = 3
        SLIC = 4
        SLICO = 5
        MSLIC = 6
        DSFH = 7
        DSERS = 8
        DSCRS = 9
        DSETPS = 10
    End Enum
    Enum LabelType
        Random
        Mean
        Outline
        OutlineOnly
    End Enum
    <DataContract()> Public Class Result
        <DataMember> Public Results As Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))

        Sub New()
            Results = New Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Integer)))
        End Sub
        Public Shared Function GetKnownTypes() As List(Of Type)
            ' returns the list of additonal types
            Return New List(Of Type) From {GetType(SegmentType), GetType(Dictionary(Of SegmentType, Integer)), GetType(Dictionary(Of Integer, Dictionary(Of SegmentType, Integer))), GetType(Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Integer))))}
        End Function
    End Class
    <DataContract()> Public Class Quantitative
        <DataMember> Public Results As Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Tuple(Of Single, Single, Single))))

        Sub New()
            Results = New Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Tuple(Of Single, Single, Single))))
        End Sub
        Public Shared Function GetKnownTypes() As List(Of Type)
            ' returns the list of additonal types
            Return New List(Of Type) From {GetType(SegmentType), GetType(Tuple(Of Single, Single, Single)), GetType(Dictionary(Of SegmentType, Tuple(Of Single, Single, Single))), GetType(Dictionary(Of Integer, Dictionary(Of SegmentType, Tuple(Of Single, Single, Single)))), GetType(Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of SegmentType, Tuple(Of Single, Single, Single)))))}
        End Function
    End Class
    Public Class SingleSmoothing
        Inherits Queue(Of Double)
        Implements IDisposable

        Private m_Capacity As Integer
        Private cacheLock As System.Threading.ReaderWriterLockSlim

        Sub New(ByVal iCapacity As Integer)
            MyBase.New(iCapacity)
            m_Capacity = iCapacity
            cacheLock = New System.Threading.ReaderWriterLockSlim
        End Sub
        Public Overloads Sub Enqueue(item As Double)
            cacheLock.EnterWriteLock()
            Try
                If Count >= m_Capacity Then
                    Do Until Count <= m_Capacity - 1
                        Dequeue()
                    Loop
                End If
                MyBase.Enqueue(item)
            Finally
                cacheLock.ExitWriteLock()
            End Try
        End Sub
        Public Function GetSmoothedQueue(ByVal fAlpha As Double) As List(Of Double)
            ' runs single exponential smoothing on the contained queue values
            Dim oQueue As New Queue(Of Double)

            If Count > 0 Then
                cacheLock.EnterReadLock()
                Try
                    For i = 0 To Count - 1
                        Select Case i
                            Case 0
                                oQueue.Enqueue(Me(i))
                            Case Else
                                Dim fS As Double = (fAlpha * Me(i)) + ((1 - fAlpha) * oQueue(i - 1))
                                oQueue.Enqueue(fS)
                        End Select
                    Next
                Finally
                    cacheLock.ExitReadLock()
                End Try
            End If

            Return oQueue.ToList
        End Function
        Public Function GetSmoothedQueueResult(ByVal fAlpha As Double) As Double
            ' get the result of the last smoothed value
            Dim oSmoothedList As List(Of Double) = GetSmoothedQueue(fAlpha)
            If oSmoothedList.Count > 0 Then
                Return oSmoothedList(oSmoothedList.Count - 1)
            Else
                Return Double.NaN
            End If
        End Function
#Region "IDisposable Support"
        Private disposedValue As Boolean
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    cacheLock.Dispose()
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            disposedValue = True
        End Sub
        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class
#Region "Serialisation"
    Private Function ReplaceExtension(ByVal sFilePath As String, ByVal sExtension As String) As String
        Try
            Return IO.Path.ChangeExtension(sFilePath, sExtension)
        Catch ex As Exception
            Return String.Empty
        End Try
    End Function
    Private Function GetKnownTypes(Optional ByVal oTypes As List(Of Type) = Nothing) As List(Of Type)
        ' gets list of known types
        Dim oKnownTypes As New List(Of Type)

        If Not oTypes Is Nothing Then
            oKnownTypes.AddRange(oTypes)
        End If
        Return oKnownTypes.Distinct.ToList
    End Function
    Private Sub SerializeDataContractStream(Of T)(ByRef oStream As IO.Stream, ByVal data As T, Optional ByVal oAdditionalTypes As List(Of Type) = Nothing, Optional ByVal bUseKnownTypes As Boolean = True)
        ' serialise to stream
        Dim oKnownTypes As New List(Of Type)
        If bUseKnownTypes Then
            oKnownTypes.AddRange(GetKnownTypes)
        End If
        If Not oAdditionalTypes Is Nothing Then
            oKnownTypes.AddRange(oAdditionalTypes)
        End If

        Dim oDataContractSerializer As New DataContractSerializer(GetType(T), oKnownTypes)
        oDataContractSerializer.WriteObject(oStream, data)
    End Sub
    Private Function DeserializeDataContractStream(Of T)(ByRef oStream As IO.Stream, Optional ByVal oAdditionalTypes As List(Of Type) = Nothing, Optional ByVal bUseKnownTypes As Boolean = True) As T
        ' deserialise from stream
        Dim oXmlDictionaryReaderQuotas As New Xml.XmlDictionaryReaderQuotas With {.MaxArrayLength = 100000000, .MaxStringContentLength = 100000000}
        Dim oXmlDictionaryReader As Xml.XmlDictionaryReader = Xml.XmlDictionaryReader.CreateTextReader(oStream, oXmlDictionaryReaderQuotas)

        Dim theObject As T = Nothing
        Try
            Dim oKnownTypes As New List(Of Type)
            If bUseKnownTypes Then
                oKnownTypes.AddRange(GetKnownTypes)
            End If
            If Not oAdditionalTypes Is Nothing Then
                oKnownTypes.AddRange(oAdditionalTypes)
            End If

            Dim oDataContractSerializer As New DataContractSerializer(GetType(T), oKnownTypes)
            theObject = oDataContractSerializer.ReadObject(oXmlDictionaryReader, True)
        Catch ex As SerializationException
        End Try

        oXmlDictionaryReader.Close()
        Return theObject
    End Function
    Private Sub SerializeDataContractFile(Of T)(ByVal sFilePath As String, ByVal data As T, Optional ByVal oAdditionalTypes As List(Of Type) = Nothing, Optional ByVal bUseKnownTypes As Boolean = True, Optional ByVal sExtension As String = "", Optional ByVal bCompress As Boolean = True)
        ' serialise using data contract serialiser
        ' compress using gzip
        ' create directory if necessary
        Dim oFileInfo As New IO.FileInfo(sFilePath)
        Dim oDirectoryInfo As IO.DirectoryInfo = oFileInfo.Directory
        If Not oDirectoryInfo.Exists Then
            oDirectoryInfo.Create()
        End If

        If bCompress Then
            Using oFileStream As IO.FileStream = IO.File.Create(If(sExtension = String.Empty, sFilePath, ReplaceExtension(sFilePath, If(sExtension = String.Empty, "gz", sExtension))))
                Using oGZipStream As New IO.Compression.GZipStream(oFileStream, IO.Compression.CompressionMode.Compress)
                    SerializeDataContractStream(oGZipStream, data, oAdditionalTypes, bUseKnownTypes)
                End Using
            End Using
        Else
            Using oFileStream As IO.FileStream = IO.File.Create(If(sExtension = String.Empty, sFilePath, ReplaceExtension(sFilePath, If(sExtension = String.Empty, "xml", sExtension))))
                SerializeDataContractStream(oFileStream, data, oAdditionalTypes, bUseKnownTypes)
            End Using
        End If
    End Sub
    Private Function DeserializeDataContractFile(Of T)(ByVal sFilePath As String, Optional ByVal oAdditionalTypes As List(Of Type) = Nothing, Optional ByVal bUseKnownTypes As Boolean = True, Optional ByVal sExtension As String = "", Optional ByVal bDecompress As Boolean = True) As T
        ' deserialise using data contract serialiser
        If bDecompress Then
            Using oFileStream As IO.FileStream = IO.File.OpenRead(If(sExtension = String.Empty, sFilePath, ReplaceExtension(sFilePath, If(sExtension = String.Empty, "gz", sExtension))))
                Using oGZipStream As New IO.Compression.GZipStream(oFileStream, IO.Compression.CompressionMode.Decompress)
                    Return DeserializeDataContractStream(Of T)(oGZipStream, oAdditionalTypes, bUseKnownTypes)
                End Using
            End Using
        Else
            Using oFileStream As IO.FileStream = IO.File.OpenRead(If(sExtension = String.Empty, sFilePath, ReplaceExtension(sFilePath, If(sExtension = String.Empty, "xml", sExtension))))
                Return DeserializeDataContractStream(Of T)(oFileStream, oAdditionalTypes, bUseKnownTypes)
            End Using
        End If
    End Function
#End Region
#Region "Declarations"
    <DllImport("ScanSegmentC.dll", EntryPoint:="initScan", CallingConvention:=CallingConvention.StdCall)> Private Sub initScan()
    End Sub
    <DllImport("ScanSegmentC.dll", EntryPoint:="exitScan", CallingConvention:=CallingConvention.StdCall)> Private Sub exitScan()
    End Sub
    <DllImport("ScanSegmentC.dll", EntryPoint:="segment", CallingConvention:=CallingConvention.StdCall)> Private Function segment(ByVal oImgStructIn As IntPtr, ByVal oImgBufferIn As IntPtr, ByVal oLabelsStructOut As IntPtr, ByVal oLabelsBufferOut As IntPtr, ByVal boundsx As Int32, ByVal boundsy As Int32, ByVal boundswidth As Int32, ByVal boundsheight As Int32, ByVal superpixels As Int32, ByVal multiplier As Single, ByVal merge As Boolean, ByVal type As Int32, ByRef duration As Int32) As Int32
    End Function
#End Region
End Module
