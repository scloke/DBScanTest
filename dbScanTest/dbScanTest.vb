Imports OfficeOpenXml
Imports Emgu.CV
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization
Imports System.Windows
Imports System.Windows.Forms
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Module dbScanTest
    Const MaxSize As Integer = 4000000
    Const MinSize As Integer = 10000
    Const SizeStep As Double = 0.1 ' log base 10 x SizeStep
    Const dbScanBigTop As Single = 5.0
    Const dbScanBigBottom As Single = 0.1
    Const dbScanBigStep As Single = 0.1
    Const dbScanSmallStep As Single = 0.01
    Const dbScanIntScale As Integer = 1000
    Const dbScanClusterCount As Integer = 50
    Const Resolution096 As Single = 96
    Const SaveDirectory As String = "D:\Downloads\"
    Const MinCluster As Single = 0.001
    Const MaxWidth As Integer = 4000
    Const MaxHeight As Integer = 4000
    Const DataStr As String = "Data"
    Const NumStr As String = "No"
    Const ScanTypeStr As String = "Scan Type"
    Const SizeStr As String = "Pixels"
    Const ToleranceStr As String = "Tolerance"
    Const TimeStr As String = "Duration"
    Const DifferenceStr As String = "Difference"
    Const ClustersStr As String = "Clusters"
    Const FileStr As String = "File"

    Private m_LUTLower As Matrix(Of Byte)
    Private m_LUTUpper As Matrix(Of Byte)

    Sub Main()
        Dim oFolderBrowserDialog As New FolderBrowserDialog
        oFolderBrowserDialog.Description = "dbScan Test Images"
        oFolderBrowserDialog.ShowNewFolderButton = False
        oFolderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop
        If oFolderBrowserDialog.ShowDialog = DialogResult.OK Then
            Dim oDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath)
            Dim oImageFiles As New List(Of IO.FileInfo)
            oImageFiles.AddRange(oDirectoryInfo.EnumerateFiles("*.jpg", IO.SearchOption.AllDirectories))
            oImageFiles.AddRange(oDirectoryInfo.EnumerateFiles("*.tif", IO.SearchOption.AllDirectories))

            If oImageFiles.Count > 0 Then
                Dim sTimingFile As String = SaveDirectory + "Timings.xml"
                Dim oTimingList As New List(Of TestGroup)
                Dim oStartDate As Date = Date.Now

                If IO.File.Exists(sTimingFile) Then
                    oTimingList = DeserializeDataContractFile(Of List(Of TestGroup))(sTimingFile, , , , False)
                    Console.WriteLine(GetElapsed(oStartDate) + " Timings Loaded")
                Else
                    Dim oSizeList As New List(Of Integer)
                    Dim iMaxStep As Integer = Math.Log10(MaxSize / MinSize) / 0.1
                    For i = 0 To iMaxStep
                        oSizeList.Add(Math.Pow(10, SizeStep * i) * MinSize)
                    Next

                    initScan(MaxWidth, MaxHeight)

                    ' initial dry run to get opencv loaded and calculate tolerance for various sizes
                    Console.WriteLine(GetElapsed(oStartDate) + " Preprocessing Start")

                    Dim oFileDict As New Dictionary(Of IO.FileInfo, Dictionary(Of Integer, Tuple(Of Single, Integer)))
                    For i = 0 To oImageFiles.Count - 1
                        Dim oScaleDict As New Dictionary(Of Integer, Tuple(Of Single, Integer))
                        Dim oImageFile As IO.FileInfo = oImageFiles(i)

                        Console.WriteLine(GetElapsed(oStartDate) + " Preprocessing File " + (i + 1).ToString + "/" + oImageFiles.Count.ToString)

                        Using oBitmap As New System.Drawing.Bitmap(oImageFile.FullName)
                            Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                CvInvoke.CvtColor(oMatrix, oMatrix, CvEnum.ColorConversion.Bgr2Lab)
                                For Each iSize In oSizeList
                                    Dim fScale As Double = Math.Sqrt(iSize / (oMatrix.Width * oMatrix.Height))
                                    Dim oSize As New System.Drawing.Size(oMatrix.Width * fScale, oMatrix.Height * fScale)

                                    Console.WriteLine(GetElapsed(oStartDate) + " Preprocessing " + oSize.Width.ToString + "x" + oSize.Height.ToString + " px")

                                    Using oScaledMatrix As New Matrix(Of Byte)(oSize.Height, oSize.Width, oMatrix.NumberOfChannels)
                                        CvInvoke.Resize(oMatrix, oScaledMatrix, oSize, 0, 0, CvEnum.Inter.Cubic)

                                        Dim oBounds As New System.Drawing.Rectangle(0, 0, oScaledMatrix.Width, oScaledMatrix.Height)
                                        Dim iMinCluster As Integer = Math.Max(MinCluster * oScaledMatrix.Width * oScaledMatrix.Height, 1)
                                        Dim iClusterID As Integer = 0
                                        Dim oLabels As Matrix(Of Integer) = Nothing

                                        Dim iScaleBottom As Integer = CInt(dbScanBigBottom * dbScanIntScale)
                                        Dim iScaleTop As Integer = CInt(dbScanBigTop * dbScanIntScale)
                                        Dim iScaleStep As Integer = CInt(dbScanBigStep * dbScanIntScale)
                                        Dim iScaleStepSmall As Integer = CInt(dbScanSmallStep * dbScanIntScale)
                                        For j = iScaleBottom To iScaleTop Step iScaleStep
                                            Dim fTolerance As Single = CSng(j) / CSng(dbScanIntScale)

                                            dbScan(oBounds, oScaledMatrix, oLabels, TestGroup.dbScanEnum.GPUInit, fTolerance, iMinCluster, iClusterID)

                                            If iClusterID <= dbScanClusterCount Then
                                                If j > iScaleBottom Then
                                                    Dim iScaleSmallBottom As Integer = j - iScaleStep
                                                    Dim iScaleSmallTop As Integer = j

                                                    For k = iScaleSmallBottom To iScaleSmallTop Step iScaleStepSmall
                                                        fTolerance = CSng(k) / CSng(dbScanIntScale)

                                                        dbScan(oBounds, oScaledMatrix, oLabels, TestGroup.dbScanEnum.GPUInit, fTolerance, iMinCluster, iClusterID)
                                                        If iClusterID <= dbScanClusterCount Then
                                                            oScaleDict.Add(iSize, New Tuple(Of Single, Integer)(fTolerance, iClusterID))
                                                            Exit For
                                                        End If
                                                    Next
                                                Else
                                                    ' just add value
                                                    oScaleDict.Add(iSize, New Tuple(Of Single, Integer)(fTolerance, iClusterID))
                                                End If
                                                Exit For
                                            End If
                                        Next

                                        If MatrixNotNothing(oLabels) Then
                                            oLabels.Dispose()
                                            oLabels = Nothing
                                        End If
                                    End Using
                                Next
                            End Using
                        End Using

                        oFileDict.Add(oImageFile, oScaleDict)
                    Next

                    If oImageFiles.Count > 0 Then
                        Console.WriteLine(GetElapsed(oStartDate) + " Output Image Files")
                        Dim oImageFile As IO.FileInfo = oImageFiles.First
                        Using oBitmap As New System.Drawing.Bitmap(oImageFile.FullName)
                            Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                CvInvoke.CvtColor(oMatrix, oMatrix, CvEnum.ColorConversion.Bgr2Lab)
                                Dim iSize = oSizeList.Last
                                Dim fScale As Double = Math.Sqrt(iSize / (oMatrix.Width * oMatrix.Height))
                                Dim oSize As New System.Drawing.Size(oMatrix.Width * fScale, oMatrix.Height * fScale)

                                Using oScaledMatrix As New Matrix(Of Byte)(oSize.Height, oSize.Width, oMatrix.NumberOfChannels)
                                    CvInvoke.Resize(oMatrix, oScaledMatrix, oSize, 0, 0, CvEnum.Inter.Cubic)

                                    Dim oBoundsSmall As New System.Drawing.Rectangle(oScaledMatrix.Width / 4, oScaledMatrix.Height / 4, oScaledMatrix.Width / 2, oScaledMatrix.Height / 2)
                                    Dim oBounds As New System.Drawing.Rectangle(0, 0, oScaledMatrix.Width, oScaledMatrix.Height)
                                    Dim oLabel As Matrix(Of Integer) = Nothing
                                    Dim iClusterID As Integer = 0

                                    Dim oEnumList As List(Of TestGroup.dbScanEnum) = [Enum].GetValues(GetType(TestGroup.dbScanEnum)).Cast(Of TestGroup.dbScanEnum).ToList
                                    Using oCompareMask As New Matrix(Of Byte)(oScaledMatrix.Size)
                                        For j = 0 To oEnumList.Count - 1
                                            Dim sEnumName As String = [Enum].GetName(GetType(TestGroup.dbScanEnum), oEnumList(j))
                                            Dim sImageFile As String = SaveDirectory + "Image_" + sEnumName + ".tif"
                                            Dim sSmallImageFile As String = SaveDirectory + "Image_Small_" + sEnumName + ".tif"

                                            If Not IO.File.Exists(sImageFile) Then
                                                dbScan(oBounds, oScaledMatrix, oLabel, oEnumList(j), oFileDict(oImageFile)(iSize).Item1, Math.Max(MinCluster * oScaledMatrix.Width * oScaledMatrix.Height, 1), iClusterID)
                                                Using oLabelByte As Matrix(Of Byte) = ConvertLabels(oLabel)
                                                    SaveMatrix(sImageFile, oLabelByte)
                                                End Using
                                            End If

                                            If Not IO.File.Exists(sSmallImageFile) Then
                                                dbScan(oBoundsSmall, oScaledMatrix, oLabel, oEnumList(j), oFileDict(oImageFile)(iSize).Item1, Math.Max(MinCluster * oScaledMatrix.Width * oScaledMatrix.Height, 1), iClusterID)
                                                Using oLabelByte As Matrix(Of Byte) = ConvertLabels(oLabel)
                                                    SaveMatrix(sSmallImageFile, oLabelByte)
                                                End Using
                                            End If
                                            Console.WriteLine(GetElapsed(oStartDate) + " Output Image File " + sEnumName)
                                        Next
                                    End Using

                                    ' clean up
                                    If MatrixNotNothing(oLabel) Then
                                        oLabel.Dispose()
                                        oLabel = Nothing
                                    End If
                                End Using
                            End Using
                        End Using
                    End If

                    For i = 0 To oImageFiles.Count - 1
                        Dim oImageFile As IO.FileInfo = oImageFiles(i)

                        Console.WriteLine(GetElapsed(oStartDate) + " Starting " + (i + 1).ToString + "/" + oImageFiles.Count.ToString + ": " + oImageFile.Name)
                        Dim oTestGroup As New TestGroup(oImageFile.Name)
                        Using oBitmap As New System.Drawing.Bitmap(oImageFile.FullName)
                            Using oMatrix As Matrix(Of Byte) = BitmapToMatrix(oBitmap)
                                CvInvoke.CvtColor(oMatrix, oMatrix, CvEnum.ColorConversion.Bgr2Lab)
                                For Each iSize In oSizeList
                                    Dim fScale As Double = Math.Sqrt(iSize / (oMatrix.Width * oMatrix.Height))
                                    Dim oSize As New System.Drawing.Size(oMatrix.Width * fScale, oMatrix.Height * fScale)

                                    Console.WriteLine(GetElapsed(oStartDate) + " Processing " + oSize.Width.ToString + "x" + oSize.Height.ToString + " px")

                                    Using oScaledMatrix As New Matrix(Of Byte)(oSize.Height, oSize.Width, oMatrix.NumberOfChannels)
                                        CvInvoke.Resize(oMatrix, oScaledMatrix, oSize, 0, 0, CvEnum.Inter.Cubic)

                                        Dim oBounds As New System.Drawing.Rectangle(0, 0, oScaledMatrix.Width, oScaledMatrix.Height)
                                        Dim oLabelsList As New List(Of Matrix(Of Integer))
                                        Dim oClusterIDList As New List(Of Integer)
                                        Dim oDurationList As New List(Of Integer)

                                        Dim oEnumList As List(Of TestGroup.dbScanEnum) = [Enum].GetValues(GetType(TestGroup.dbScanEnum)).Cast(Of TestGroup.dbScanEnum).ToList
                                        Dim iDifferenceCount As Integer = 0
                                        Dim fDifference As Single = 0
                                        Using oCompareMask As New Matrix(Of Byte)(oScaledMatrix.Size)
                                            For j = 0 To oEnumList.Count - 1
                                                oLabelsList.Add(Nothing)
                                                oClusterIDList.Add(0)
                                                oDurationList.Add(dbScan(oBounds, oScaledMatrix, oLabelsList(j), oEnumList(j), oFileDict(oImageFile)(iSize).Item1, Math.Max(MinCluster * oScaledMatrix.Width * oScaledMatrix.Height, 1), oClusterIDList(j)))
                                                Console.WriteLine(GetElapsed(oStartDate) + " Processing " + [Enum].GetName(GetType(TestGroup.dbScanEnum), oEnumList(j)) + " Done")

                                                If oEnumList(j) = TestGroup.dbScanEnum.Original Then
                                                    oTestGroup.Results.Add(iSize, New Tuple(Of Dictionary(Of TestGroup.dbScanEnum, TestGroup.TestResults), Single)(New Dictionary(Of TestGroup.dbScanEnum, TestGroup.TestResults), oFileDict(oImageFile)(iSize).Item1))
                                                    oTestGroup.Results(iSize).Item1.Add(TestGroup.dbScanEnum.Original, New TestGroup.TestResults(oDurationList(j), 0.0, oClusterIDList(j)))
                                                Else
                                                    CvInvoke.Compare(oLabelsList(0), oLabelsList(j), oCompareMask, CvEnum.CmpType.NotEqual)
                                                    iDifferenceCount = CvInvoke.CountNonZero(oCompareMask)
                                                    fDifference = CSng(iDifferenceCount) / CSng(oScaledMatrix.Width * oScaledMatrix.Height)
                                                    oTestGroup.Results(iSize).Item1.Add(oEnumList(j), New TestGroup.TestResults(oDurationList(j), fDifference, oClusterIDList(j)))
                                                End If
                                            Next
                                        End Using

                                        ' clean up
                                        For j = 0 To oEnumList.Count - 1
                                            If MatrixNotNothing(oLabelsList(j)) Then
                                                oLabelsList(j).Dispose()
                                                oLabelsList(j) = Nothing
                                            End If
                                        Next
                                    End Using
                                Next
                            End Using
                        End Using
                        oTimingList.Add(oTestGroup)
                    Next

                    exitScan()

                    SerializeDataContractFile(sTimingFile, oTimingList, , , , False)

                    Console.WriteLine(GetElapsed(oStartDate) + " Timings Saved")
                End If

                ' process to excel file
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial

                Using oDataDocument As New ExcelPackage()
                    oDataDocument.Workbook.Worksheets.Add(DataStr)
                    Using oDataSheet As ExcelWorksheet = oDataDocument.Workbook.Worksheets(0)
                        Const iNumCol As Integer = 1
                        Const iScanTypeCol As Integer = 2
                        Const iSizeCol As Integer = 3
                        Const iToleranceCol As Integer = 4
                        Const iTimeCol As Integer = 5
                        Const iDifferenceCol As Integer = 6
                        Const iClustersCol As Integer = 7
                        Const iFileCol As Integer = 8

                        oDataSheet.SetValue(1, iNumCol, NumStr)
                        oDataSheet.SetValue(1, iScanTypeCol, ScanTypeStr)
                        oDataSheet.SetValue(1, iSizeCol, SizeStr)
                        oDataSheet.SetValue(1, iToleranceCol, ToleranceStr)
                        oDataSheet.SetValue(1, iTimeCol, TimeStr)
                        oDataSheet.SetValue(1, iDifferenceCol, DifferenceStr)
                        oDataSheet.SetValue(1, iClustersCol, ClustersStr)
                        oDataSheet.SetValue(1, iFileCol, FileStr)

                        Dim iCurrentRow As Integer = 2
                        For Each oGroup In oTimingList
                            For Each oResult In oGroup.Results
                                For Each oTestResult In oResult.Value.Item1
                                    oDataSheet.SetValue(iCurrentRow, iNumCol, iCurrentRow - 1)
                                    oDataSheet.SetValue(iCurrentRow, iScanTypeCol, [Enum].GetName(GetType(TestGroup.dbScanEnum), oTestResult.Key))
                                    oDataSheet.SetValue(iCurrentRow, iSizeCol, oResult.Key)
                                    oDataSheet.SetValue(iCurrentRow, iToleranceCol, oResult.Value.Item2)
                                    oDataSheet.SetValue(iCurrentRow, iTimeCol, oTestResult.Value.Time)
                                    oDataSheet.SetValue(iCurrentRow, iDifferenceCol, oTestResult.Value.Difference)
                                    oDataSheet.SetValue(iCurrentRow, iClustersCol, oTestResult.Value.Clusters)
                                    oDataSheet.SetValue(iCurrentRow, iFileCol, oGroup.Name)
                                    iCurrentRow += 1
                                Next
                            Next
                        Next

                        ' autofit columns
                        oDataSheet.Cells(oDataSheet.Dimension.Start.Row, oDataSheet.Dimension.Start.Column, oDataSheet.Dimension.End.Row, oDataSheet.Dimension.End.Column).AutoFitColumns()

                        Dim sDataFile As String = SaveDirectory + "DbScanData.xlsx"
                        Dim oDataInfo As New IO.FileInfo(sDataFile)
                        If oDataInfo.Exists Then
                            oDataInfo.Delete()
                        End If

                        Console.WriteLine(GetElapsed(oStartDate) + " Saving File " + oDataInfo.Name)
                        oDataDocument.SaveAs(oDataInfo)
                    End Using
                End Using
            End If
        End If
    End Sub
    Private Function dbScan(ByVal oBounds As System.Drawing.Rectangle, ByVal oMatrixIn As Matrix(Of Byte), ByRef oLabelsOut As Matrix(Of Integer), ByVal oScanType As TestGroup.dbScanEnum, ByVal fToleranceValue As Single, ByVal iMinCluster As Integer, ByRef iClusterID As Integer) As Integer
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
        iClusterID = dbScan(oMatPointerIn, oMatBufferHandleIn.AddrOfPinnedObject, oLabelsPointerOut, oLabelsBufferHandleOut.AddrOfPinnedObject, oCroppedBounds.X, oCroppedBounds.Y, oCroppedBounds.Width, oCroppedBounds.Height, oScanType, fToleranceValue, iMinCluster, iDuration)

        Marshal.FreeCoTaskMem(oMatPointerIn)
        oMatBufferHandleIn.Free()

        Marshal.FreeCoTaskMem(oLabelsPointerOut)
        oLabelsBufferHandleOut.Free()

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
    Private Function ConvertLabels(ByVal oLabels As Matrix(Of Integer)) As Matrix(Of Byte)
        ' converts integer labels to colour matrix
        If MatrixIsNothing(m_LUTLower) OrElse MatrixIsNothing(m_LUTUpper) Then
            m_LUTLower = New Matrix(Of Byte)(256, 1, 3)
            m_LUTUpper = New Matrix(Of Byte)(256, 1, 3)
            Using oBLUT As New Matrix(Of Byte)(256, 1)
                Using oGLUT As New Matrix(Of Byte)(256, 1)
                    Using oRLUT As New Matrix(Of Byte)(256, 1)
                        For y = 0 To 255
                            oBLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                            oGLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                            oRLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                        Next

                        CvInvoke.Merge(New Util.VectorOfMat({oBLUT.Mat, oGLUT.Mat, oRLUT.Mat}), m_LUTLower)
                    End Using
                End Using
            End Using
            Using oBLUT As New Matrix(Of Byte)(256, 1)
                Using oGLUT As New Matrix(Of Byte)(256, 1)
                    Using oRLUT As New Matrix(Of Byte)(256, 1)
                        For y = 0 To 255
                            oBLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                            oGLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                            oRLUT(y, 0) = Int((255 - 127 + 1) * Rnd() + 127)
                        Next

                        CvInvoke.Merge(New Util.VectorOfMat({oBLUT.Mat, oGLUT.Mat, oRLUT.Mat}), m_LUTUpper)
                    End Using
                End Using
            End Using
        End If

        ' convert to colour matrices
        Dim oLabelsOut As Matrix(Of Byte) = Nothing
        If MatrixNotNothing(oLabels) Then
            oLabelsOut = New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
            Using oLabelsUpper As New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
                Using oLabelsLower As New Matrix(Of Byte)(oLabels.Height, oLabels.Width, 3)
                    Using oLabelsByte As Matrix(Of Integer) = oLabels.Clone
                        oLabelsByte._Mul(1 / 256.0)
                        CvInvoke.CvtColor(oLabelsByte.Convert(Of Byte), oLabelsUpper, CvEnum.ColorConversion.Gray2Bgr)
                        CvInvoke.LUT(oLabelsUpper, m_LUTUpper, oLabelsUpper)

                        oLabelsByte._Mul(256.0)
                        CvInvoke.Subtract(oLabels, oLabelsByte, oLabelsByte)
                        CvInvoke.CvtColor(oLabelsByte.Convert(Of Byte), oLabelsLower, CvEnum.ColorConversion.Gray2Bgr)
                        CvInvoke.LUT(oLabelsLower, m_LUTLower, oLabelsLower)
                    End Using
                    CvInvoke.BitwiseXor(oLabelsUpper, oLabelsLower, oLabelsOut)
                    oLabelsOut.SubR(255).CopyTo(oLabelsOut)
                End Using
            End Using
        End If
        Return oLabelsOut
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
    <DataContract()> Public Class TestGroup
        ' key = size from list, value tuple = tolerance, cluster count
        <DataMember> Public Name As String
        <DataMember> Public Results As Dictionary(Of Integer, Tuple(Of Dictionary(Of dbScanEnum, TestResults), Single))
        Sub New(ByVal sName As String)
            Name = sName
            Results = New Dictionary(Of Integer, Tuple(Of Dictionary(Of dbScanEnum, TestResults), Single))
        End Sub
        Public Shared Function GetKnownTypes() As List(Of Type)
            ' returns the list of additonal types
            Return New List(Of Type) From {GetType(dbScanEnum), GetType(TestResults), GetType(Dictionary(Of dbScanEnum, TestResults)), GetType(Tuple(Of Single, Integer)), GetType(Dictionary(Of System.Drawing.Size, Tuple(Of Dictionary(Of dbScanEnum, TestResults), Tuple(Of Single, Integer))))}
        End Function
        <DataContract()> Public Class TestResults
            <DataMember> Public Time As Integer ' in microseconds
            <DataMember> Public Difference As Single ' from original in percentage of pixels
            <DataMember> Public Clusters As Integer ' number of different clusters

            Sub New(ByVal iTime As Integer, ByVal fDifference As Single, ByVal iClusters As Integer)
                Time = iTime
                Difference = fDifference
                Clusters = iClusters
            End Sub
            Public Shared Function GetKnownTypes() As List(Of Type)
                ' returns the list of additonal types
                Return New List(Of Type) From {}
            End Function
        End Class
        Public Enum dbScanEnum As Int32
            Original = 0
            CIEDE2000 = 1
            GPU = 2
            GPUInit = 3
            GPUParallel = 4
            CPU = 5
            CPUInit = 6
            GPUNoise = 7
        End Enum
    End Class
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
    <DllImport("dbScanTestC.dll", EntryPoint:="initScan", CallingConvention:=CallingConvention.StdCall)> Private Sub initScan(ByVal maxwidth As Int32, ByVal maxheight As Int32)
    End Sub
    <DllImport("dbScanTestC.dll", EntryPoint:="exitScan", CallingConvention:=CallingConvention.StdCall)> Private Sub exitScan()
    End Sub
    <DllImport("dbScanTestC.dll", EntryPoint:="dbScan", CallingConvention:=CallingConvention.StdCall)> Private Function dbScan(ByVal oImgStructIn As IntPtr, ByVal oImgBufferIn As IntPtr, ByVal oLabelsStructOut As IntPtr, ByVal oLabelsBufferOut As IntPtr, ByVal boundsx As Int32, ByVal boundsy As Int32, ByVal boundswidth As Int32, ByVal boundsheight As Int32, ByVal scantype As Int32, ByVal toleranceValue As Single, ByVal minCluster As Int32, ByRef duration As Int32) As Int32
    End Function
#End Region
End Module
