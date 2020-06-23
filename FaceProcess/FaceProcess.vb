Imports Emgu.CV
Imports System.Windows
Imports System.Windows.Forms
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Module FaceProcess
    Const FaceBackground As String = "FaceBackground.jpg"
    Const FaceDirectory As String = "Faces"
    Const ProcessedDirectory As String = "FacesProcessed"
    Const Resolution096 As Single = 96

    Sub Main()
        Dim oFolderBrowserDialog As New FolderBrowserDialog
        oFolderBrowserDialog.Description = "dbScan Face Images"
        oFolderBrowserDialog.ShowNewFolderButton = False
        oFolderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop
        If oFolderBrowserDialog.ShowDialog = DialogResult.OK Then
            Dim oDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath)
            Dim oBackgroundFiles As List(Of IO.FileInfo) = oDirectoryInfo.EnumerateFiles(FaceBackground).ToList
            Dim oFaceDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + "\" + FaceDirectory)
            Dim oFaceDirectoryFiles As List(Of IO.FileInfo) = oFaceDirectoryInfo.EnumerateFiles("*.png").ToList

            If oBackgroundFiles.Count = 1 AndAlso oFaceDirectoryInfo.Exists AndAlso oFaceDirectoryFiles.Count > 0 Then
                Dim oStartDate As Date = Date.Now

                Dim oProcessedDirectoryInfo As New IO.DirectoryInfo(oFolderBrowserDialog.SelectedPath + "\" + ProcessedDirectory)
                If oProcessedDirectoryInfo.Exists Then
                    oProcessedDirectoryInfo.Delete(True)
                End If
                oProcessedDirectoryInfo.Create()

                Using oBackgroundBitmap As New System.Drawing.Bitmap(oBackgroundFiles.First.FullName)
                    Using oBackgroundMatrixBGR As Matrix(Of Byte) = BitmapToMatrix(oBackgroundBitmap)
                        For Each oFaceFile In oFaceDirectoryFiles
                            Console.WriteLine(GetElapsed(oStartDate) + " Processing File " + (oFaceDirectoryFiles.IndexOf(oFaceFile) + 1).ToString + "/" + oFaceDirectoryFiles.Count.ToString)


                            Dim sNewFaceFile As String = oFolderBrowserDialog.SelectedPath + "\" + ProcessedDirectory + "\" + Left(oFaceFile.Name, Len(oFaceFile.Name) - Len(oFaceFile.Extension)) + "_Processed.tif"

                            Using oFaceBitmap As New System.Drawing.Bitmap(oFaceFile.FullName)
                                If oFaceBitmap.PixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb Then
                                    Using oFaceMatrixBGRA As Matrix(Of Byte) = BitmapToMatrix32(oFaceBitmap)
                                        Using oFaceMatrixBGR As New Matrix(Of Byte)(oFaceMatrixBGRA.Height, oFaceMatrixBGRA.Width, 3)
                                            Using oFaceMatrixA As New Matrix(Of Byte)(oFaceMatrixBGRA.Height, oFaceMatrixBGRA.Width)
                                                Using oVectorMat As New Util.VectorOfMat
                                                    CvInvoke.Split(oFaceMatrixBGRA.Mat, oVectorMat)
                                                    CvInvoke.Merge(New Util.VectorOfMat({oVectorMat(0), oVectorMat(1), oVectorMat(2)}), oFaceMatrixBGR)
                                                    oVectorMat(3).CopyTo(oFaceMatrixA)

                                                    Using oFaceMatrixSingleBGR As Matrix(Of Single) = oFaceMatrixBGR.Convert(Of Single)
                                                        Using oFaceMatrixSingleA As Matrix(Of Single) = oFaceMatrixA.Convert(Of Single)(1.0 / 255.0)
                                                            Using oFaceMatrixSingleAAA As New Matrix(Of Single)(oFaceMatrixSingleA.Height, oFaceMatrixSingleA.Width, 3)
                                                                CvInvoke.CvtColor(oFaceMatrixSingleA, oFaceMatrixSingleAAA, CvEnum.ColorConversion.Gray2Bgr)
                                                                Dim iDispX As Integer = Int(((oBackgroundMatrixBGR.Width - oFaceMatrixBGRA.Width) + 1) * Rnd())
                                                                Dim iDispY As Integer = Int(((oBackgroundMatrixBGR.Height - oFaceMatrixBGRA.Height) + 1) * Rnd())

                                                                Dim oDispRect As New System.Drawing.Rectangle(iDispX, iDispY, oFaceMatrixBGRA.Width, oFaceMatrixBGRA.Height)
                                                                Using oCompositeBGR As Matrix(Of Single) = oBackgroundMatrixBGR.GetSubRect(oDispRect).Convert(Of Single)
                                                                    CvInvoke.Multiply(oCompositeBGR, oFaceMatrixSingleAAA.SubR(1.0), oCompositeBGR)
                                                                    CvInvoke.Multiply(oFaceMatrixSingleBGR, oFaceMatrixSingleAAA, oFaceMatrixSingleBGR)
                                                                    CvInvoke.Add(oCompositeBGR, oFaceMatrixSingleBGR, oCompositeBGR)

                                                                    SaveMatrix(sNewFaceFile, oCompositeBGR.Convert(Of Byte))
                                                                End Using
                                                            End Using
                                                        End Using
                                                    End Using
                                                End Using
                                            End Using
                                        End Using
                                    End Using
                                End If
                            End Using
                        Next
                    End Using
                End Using
            End If
        End If
    End Sub
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
    Private Function BitmapToMatrix32(ByVal oBitmap As System.Drawing.Bitmap) As Matrix(Of Byte)
        ' convert bitmap to matrix
        If IsNothing(oBitmap) Then
            Return Nothing
        Else
            Dim oReturnMatrix As Matrix(Of Byte) = Nothing
            Dim oRectangle As New System.Drawing.Rectangle(0, 0, oBitmap.Width, oBitmap.Height)
            Dim oBitmapData As System.Drawing.Imaging.BitmapData = oBitmap.LockBits(oRectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly, oBitmap.PixelFormat)

            If oBitmap.PixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb Then
                Using oMat As New Mat(oBitmap.Height, oBitmap.Width, CvEnum.DepthType.Cv8U, 4, oBitmapData.Scan0, oBitmapData.Stride)
                    oReturnMatrix = New Matrix(Of Byte)(oBitmap.Height, oBitmap.Width, 4)
                    oMat.CopyTo(oReturnMatrix)
                End Using
            Else
                Return Nothing
            End If

            oBitmap.UnlockBits(oBitmapData)

            Return oReturnMatrix
        End If
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
End Module
