// dbScanTestC.cpp : Defines the exported functions for the DLL.

#include "pch.h" // use pch.h in Visual Studio 2019

#include "dbScanTestC.h"

dbScanOriginal* scan0 = NULL;
dbScanCIEDE2000* scan1 = NULL;
dbScanGPU* scan2 = NULL;
dbScanGPUInit* scan3 = NULL;
dbScanGPUPar* scan4 = NULL;
dbScanCPU* scan5 = NULL;
dbScanCPUInit* scan6 = NULL;
dbScanGPUNoise* scan7 = NULL;
static const int setconcurrency = 4;

void initScan(int maxwidth, int maxheight)
{
	// check thread concurrency
	int concurrentthreads = std::thread::hardware_concurrency();
	if (concurrentthreads == 0) {
		concurrentthreads = setconcurrency;
	}

	if (scan0 == NULL) {
		scan0 = new dbScanOriginal();
	}
	if (scan1 == NULL) {
		scan1 = new dbScanCIEDE2000();
	}
	if (scan2 == NULL) {
		scan2 = new dbScanGPU();
	}
	if (scan3 == NULL) {
		scan3 = new dbScanGPUInit(maxwidth, maxheight);
	}
	if (scan4 == NULL) {
		scan4 = new dbScanGPUPar(concurrentthreads);
	}
	if (scan5 == NULL) {
		scan5 = new dbScanCPU();
	}
	if (scan6 == NULL) {
		scan6 = new dbScanCPUInit(maxwidth, maxheight);
	}
	if (scan7 == NULL) {
		scan7 = new dbScanGPUNoise();
	}
}

void exitScan()
{
	if (scan0 != NULL) {
		delete scan0;
		scan0 = NULL;
	}

	if (scan1 != NULL) {
		delete scan1;
		scan1 = NULL;
	}

	if (scan2 != NULL) {
		delete scan2;
		scan2 = NULL;
	}

	if (scan3 != NULL) {
		delete scan3;
		scan3 = NULL;
	}

	if (scan4 != NULL) {
		delete scan4;
		scan4 = NULL;
	}

	if (scan5 != NULL) {
		delete scan5;
		scan5 = NULL;
	}

	if (scan6 != NULL) {
		delete scan6;
		scan6 = NULL;
	}

	if (scan7 != NULL) {
		delete scan7;
		scan7 = NULL;
	}
}

// entrance function
// 0 - original dbScan, 1 - dbScanGPU, 2 - dbScanGPUParallel
int dbScan(BYTE* imgstruct_in, BYTE* imgbuffer_in, BYTE* imgstructlabels_out, BYTE* imgbufferlabels_out, int boundsx, int boundsy, int boundswidth, int boundsheight, int scantype, float toleranceValue, int minCluster, int* duration)
{
	cv::Mat img_input = utility::getMat(imgstruct_in, imgbuffer_in);
	cv::Mat img_labels = utility::getMat(imgstructlabels_out, imgbufferlabels_out);
	cv::Rect bounds(boundsx, boundsy, boundswidth, boundsheight);
	
	int clusterID = 0;

	switch (scantype) {
	case 0:
	{
		clusterID = scan0->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	case 1:
	{
		clusterID = scan1->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	case 2:
	{
		clusterID = scan2->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	case 3:
	{
		clusterID = scan3->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	case 4:
	{
		clusterID = scan4->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	case 5:
	{
		clusterID = scan5->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	case 6:
	{
		clusterID = scan6->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	case 7:
	{
		clusterID = scan7->scan(img_input, bounds, &img_labels, toleranceValue, minCluster, duration);
		break;
	}
	default:
	{
		break;
	}
	}

	return clusterID;
}

// CIEDE FUNCTIONS
float CIEDE::getE76Byte(cv::Vec3b labB, cv::Vec3b plabB)
{
	// do not multiply by E76ADH
	// convert to L 0 to 100, A -128 to 128, B -128 to 128
	cv::Vec3f lab(MIN((float)labB[0] * 100.0f / 255.0f, 100.0f), ((float)labB[1] - 128.0f), ((float)labB[2] - 128.0f));
	cv::Vec3f plab(MIN((float)plabB[0] * 100.0f / 255.0f, 100.0f), ((float)plabB[1] - 128.0f), ((float)plabB[2] - 128.0f));

	return sqrtf((lab[0] - plab[0]) * (lab[0] - plab[0]) + (lab[1] - plab[1]) * (lab[1] - plab[1]) + (lab[2] - plab[2]) * (lab[2] - plab[2]));
}

float CIEDE::getCIEDE2000(cv::Vec3b labB, cv::Vec3b plabB, double k_L, double k_C, double k_H)
{
	// convert to L 0 to 100, A -128 to 128, B -128 to 128
	cv::Vec3d lab(MIN((double)labB[0] * 100.0 / 255.0, 100.0), (double)labB[1] - 128.0, (double)labB[2] - 128.0);
	cv::Vec3d plab(MIN((double)plabB[0] * 100.0 / 255.0, 100.0), (double)plabB[1] - 128.0, (double)plabB[2] - 128.0);

	/*
	 * "For these and all other numerical/graphical delta E00 values
	 * reported in this article, we set the parametric weighting factors
	 * to unity(i.e., k_L = k_C = k_H = 1.0)." (Page 27).
	 */
	const double deg360InRad = deg2Rad(360.0);
	const double deg180InRad = deg2Rad(180.0);
	const double pow25To7 = 6103515625.0; /* pow(25, 7) */

	/*
	 * Step 1
	 */
	 /* Equation 2 */
	double C1 = sqrt((lab[1] * lab[1]) + (lab[2] * lab[2]));
	double C2 = sqrt((plab[1] * plab[1]) + (plab[2] * plab[2]));
	/* Equation 3 */
	double barC = (C1 + C2) / 2.0;
	/* Equation 4 */
	double G = 0.5 * (1 - sqrt(pow(barC, 7) / (pow(barC, 7) + pow25To7)));
	/* Equation 5 */
	double a1Prime = (1.0 + G) * lab[1];
	double a2Prime = (1.0 + G) * plab[1];
	/* Equation 6 */
	double CPrime1 = sqrt((a1Prime * a1Prime) + (lab[2] * lab[2]));
	double CPrime2 = sqrt((a2Prime * a2Prime) + (plab[2] * plab[2]));
	/* Equation 7 */
	double hPrime1;
	if (lab[2] == 0 && a1Prime == 0)
		hPrime1 = 0.0;
	else {
		hPrime1 = atan2(lab[2], a1Prime);
		/*
		 * This must be converted to a hue angle in degrees between 0
		 * and 360 by addition of 2 to negative hue angles.
		 */
		if (hPrime1 < 0)
			hPrime1 += deg360InRad;
	}
	double hPrime2;
	if (plab[2] == 0 && a2Prime == 0)
		hPrime2 = 0.0;
	else {
		hPrime2 = atan2(plab[2], a2Prime);
		/*
		 * This must be converted to a hue angle in degrees between 0
		 * and 360 by addition of 2 to negative hue angles.
		 */
		if (hPrime2 < 0)
			hPrime2 += deg360InRad;
	}

	/*
	 * Step 2
	 */
	 /* Equation 8 */
	double deltaLPrime = plab[0] - lab[0];
	/* Equation 9 */
	double deltaCPrime = CPrime2 - CPrime1;
	/* Equation 10 */
	double deltahPrime;
	double CPrimeProduct = CPrime1 * CPrime2;
	if (CPrimeProduct == 0)
		deltahPrime = 0;
	else {
		/* Avoid the fabs() call */
		deltahPrime = hPrime2 - hPrime1;
		if (deltahPrime < -deg180InRad)
			deltahPrime += deg360InRad;
		else if (deltahPrime > deg180InRad)
			deltahPrime -= deg360InRad;
	}
	/* Equation 11 */
	double deltaHPrime = 2.0 * sqrt(CPrimeProduct) *
		sin(deltahPrime / 2.0);

	/*
	 * Step 3
	 */
	 /* Equation 12 */
	double barLPrime = (lab[0] + plab[0]) / 2.0;
	/* Equation 13 */
	double barCPrime = (CPrime1 + CPrime2) / 2.0;
	/* Equation 14 */
	double barhPrime, hPrimeSum = hPrime1 + hPrime2;
	if (CPrime1 * CPrime2 == 0) {
		barhPrime = hPrimeSum;
	}
	else {
		if (fabs(hPrime1 - hPrime2) <= deg180InRad)
			barhPrime = hPrimeSum / 2.0;
		else {
			if (hPrimeSum < deg360InRad)
				barhPrime = (hPrimeSum + deg360InRad) / 2.0;
			else
				barhPrime = (hPrimeSum - deg360InRad) / 2.0;
		}
	}
	/* Equation 15 */
	double T = 1.0 - (0.17 * cos(barhPrime - deg2Rad(30.0))) +
		(0.24 * cos(2.0 * barhPrime)) +
		(0.32 * cos((3.0 * barhPrime) + deg2Rad(6.0))) -
		(0.20 * cos((4.0 * barhPrime) - deg2Rad(63.0)));
	/* Equation 16 */
	double deltaTheta = deg2Rad(30.0) *
		exp(-pow((barhPrime - deg2Rad(275.0)) / deg2Rad(25.0), 2.0));
	/* Equation 17 */
	double R_C = 2.0 * sqrt(pow(barCPrime, 7.0) /
		(pow(barCPrime, 7.0) + pow25To7));
	/* Equation 18 */
	double S_L = 1 + ((0.015 * pow(barLPrime - 50.0, 2.0)) /
		sqrt(20 + pow(barLPrime - 50.0, 2.0)));
	/* Equation 19 */
	double S_C = 1 + (0.045 * barCPrime);
	/* Equation 20 */
	double S_H = 1 + (0.015 * barCPrime * T);
	/* Equation 21 */
	double R_T = (-sin(2.0 * deltaTheta)) * R_C;

	/* Equation 22 */
	float deltaE = (float)sqrt(
		pow(deltaLPrime / (k_L * S_L), 2.0) +
		pow(deltaCPrime / (k_C * S_C), 2.0) +
		pow(deltaHPrime / (k_H * S_H), 2.0) +
		(R_T * (deltaCPrime / (k_C * S_C)) * (deltaHPrime / (k_H * S_H))));

	return (deltaE);
}

// common pathway in all E76 mat processing to normalise lab mats into float vectors
void CIEDE::normaliseLab(const cv::UMat& labU, std::vector<cv::UMat>* diffFloatVecB, float borderinit)
{
	*diffFloatVecB = std::vector<cv::UMat>(labChannels);
	std::vector<cv::UMat> diffVec;
	cv::split(labU, diffVec);

	cv::Rect windowB(1, 1, labU.cols, labU.rows);

	for (int i = 0; i < labChannels; i++) {
		(*diffFloatVecB)[i] = cv::UMat(labU.rows + 2, labU.cols + 2, CV_32FC1);
		(*diffFloatVecB)[i].row(0).setTo(borderinit);
		(*diffFloatVecB)[i].row((*diffFloatVecB)[i].rows - 1).setTo(borderinit);
		(*diffFloatVecB)[i].col(0).setTo(borderinit);
		(*diffFloatVecB)[i].col((*diffFloatVecB)[i].cols - 1).setTo(borderinit);
		diffVec[i].convertTo((*diffFloatVecB)[i](windowB), CV_32F);
	}

	// convert to L 0 to 100, A -128 to 128, B -128 to 128
	cv::multiply((*diffFloatVecB)[0](windowB), cv::Scalar(100.0f / 255.0f), (*diffFloatVecB)[0](windowB));
	cv::min((*diffFloatVecB)[0](windowB), cv::Scalar(100.0f), (*diffFloatVecB)[0](windowB));
	cv::subtract((*diffFloatVecB)[1](windowB), cv::Scalar(128.0f), (*diffFloatVecB)[1](windowB));
	cv::subtract((*diffFloatVecB)[2](windowB), cv::Scalar(128.0f), (*diffFloatVecB)[2](windowB));

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffVec[i].release();
	}
}

// gets luminance multiplier, premultiplied with E76ADH
cv::UMat CIEDE::getMultiplier(const cv::UMat& lumU, const cv::Rect& ROI)
{
	double meanLuminance = cv::mean(lumU(ROI))[0];

	const double adjustment = 0.5;
	double adjLuminance = 255.0 - ((255.0 - meanLuminance) * adjustment);

	// get luminance multiplier -> truncate at meanLuminance -> normalise to 0-1
	cv::UMat multiplierU(lumU.rows, lumU.cols, CV_32FC1);
	multiplierU.setTo(E76ADH / 2.0f);
	cv::threshold(lumU(ROI), multiplierU(ROI), adjLuminance, 255.0, cv::THRESH_TRUNC);
	cv::normalize(multiplierU(ROI), multiplierU(ROI), 0.0f, E76ADH, cv::NORM_MINMAX);

	return multiplierU;
}

// makes an E76 colour comparison between two normalised UMats
void CIEDE::compareNormalisedE76UMat(const std::vector<cv::UMat>& diffFloatVec, const cv::UMat& multiplier, const cv::Rect& ROI1, const cv::Rect& ROI2, cv::UMat* returnUMat)
{
	assert(diffFloatVec.size() == labChannels);
	assert(ROI1.width == ROI2.width && ROI1.height == ROI2.height);

	std::vector<cv::UMat> diffFloatTempVec(labChannels);
	for (int i = 0; i < labChannels; i++) {
		cv::subtract(diffFloatVec[i](ROI1), diffFloatVec[i](ROI2), diffFloatTempVec[i]);
		cv::multiply(diffFloatTempVec[i], diffFloatTempVec[i], diffFloatTempVec[i]);
	}

	cv::UMat multiplierAve;
	cv::add(diffFloatTempVec[1], diffFloatTempVec[0], diffFloatTempVec[0]);
	cv::add(diffFloatTempVec[2], diffFloatTempVec[0], diffFloatTempVec[0]);
	cv::sqrt(diffFloatTempVec[0], diffFloatTempVec[0]);
	cv::addWeighted(multiplier(ROI1), 0.5, multiplier(ROI2), 0.5, 0.0, multiplierAve);
	cv::multiply(diffFloatTempVec[0], multiplierAve, *returnUMat);

	// clean up
	multiplierAve.release();
	for (int i = 0; i < labChannels; i++) {
		diffFloatTempVec[i].release();
	}
}

// common pathway in all E76 mat processing to normalise lab mats into float vectors
void CIEDE::normaliseLab(const cv::Mat& lab, std::vector<cv::Mat>* diffFloatVecB, float borderinit)
{
	*diffFloatVecB = std::vector<cv::Mat>(labChannels);
	std::vector<cv::Mat> diffVec;
	cv::split(lab, diffVec);

	cv::Rect windowB(1, 1, lab.cols, lab.rows);

	for (int i = 0; i < labChannels; i++) {
		(*diffFloatVecB)[i] = cv::Mat(lab.rows + 2, lab.cols + 2, CV_32FC1);
		(*diffFloatVecB)[i].row(0).setTo(borderinit);
		(*diffFloatVecB)[i].row((*diffFloatVecB)[i].rows - 1).setTo(borderinit);
		(*diffFloatVecB)[i].col(0).setTo(borderinit);
		(*diffFloatVecB)[i].col((*diffFloatVecB)[i].cols - 1).setTo(borderinit);
		diffVec[i].convertTo((*diffFloatVecB)[i](windowB), CV_32F);
	}

	// convert to L 0 to 100, A -128 to 128, B -128 to 128
	cv::multiply((*diffFloatVecB)[0](windowB), cv::Scalar(100.0f / 255.0f), (*diffFloatVecB)[0](windowB));
	cv::min((*diffFloatVecB)[0](windowB), cv::Scalar(100.0f), (*diffFloatVecB)[0](windowB));
	cv::subtract((*diffFloatVecB)[1](windowB), cv::Scalar(128.0f), (*diffFloatVecB)[1](windowB));
	cv::subtract((*diffFloatVecB)[2](windowB), cv::Scalar(128.0f), (*diffFloatVecB)[2](windowB));

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffVec[i].release();
	}
}

// gets luminance multiplier, premultiplied with E76ADH
cv::Mat CIEDE::getMultiplier(const cv::Mat& lum, const cv::Rect& ROI)
{
	double meanLuminance = cv::mean(lum(ROI))[0];

	const double adjustment = 0.5;
	double adjLuminance = 255.0 - ((255.0 - meanLuminance) * adjustment);

	// get luminance multiplier -> truncate at meanLuminance -> normalise to 0-1
	cv::Mat multiplier(lum.rows, lum.cols, CV_32FC1);
	multiplier.setTo(E76ADH / 2.0f);
	cv::threshold(lum(ROI), multiplier(ROI), adjLuminance, 255.0, cv::THRESH_TRUNC);
	cv::normalize(multiplier(ROI), multiplier(ROI), 0.0f, E76ADH, cv::NORM_MINMAX);

	return multiplier;
}

// gets luminance multiplier, premultiplied with E76ADH
cv::UMat CIEDE::getMultiplier(const cv::UMat& lumU)
{
	double meanLuminance = cv::mean(lumU)[0];

	const double adjustment = 0.5;
	double adjLuminance = 255.0 - ((255.0 - meanLuminance) * adjustment);

	// get luminance multiplier -> truncate at meanLuminance -> normalise to 0-1
	cv::UMat multiplierU;
	cv::threshold(lumU, multiplierU, adjLuminance, 255.0, cv::THRESH_TRUNC);
	cv::normalize(multiplierU, multiplierU, 0.0f, E76ADH, cv::NORM_MINMAX);

	return multiplierU;
}

// makes an E76 colour comparison between two normalised UMats
void CIEDE::compareNormalisedE76UMat(const std::vector<cv::Mat>& diffFloatVec, const cv::Mat& multiplier, const cv::Rect& ROI1, const cv::Rect& ROI2, cv::Mat* returnMat)
{
	assert(diffFloatVec.size() == labChannels);
	assert(ROI1.width == ROI2.width && ROI1.height == ROI2.height);

	std::vector<cv::Mat> diffFloatTempVec(labChannels);
	for (int i = 0; i < labChannels; i++) {
		cv::subtract(diffFloatVec[i](ROI1), diffFloatVec[i](ROI2), diffFloatTempVec[i]);
		cv::multiply(diffFloatTempVec[i], diffFloatTempVec[i], diffFloatTempVec[i]);
	}

	cv::Mat multiplierAve;
	cv::add(diffFloatTempVec[1], diffFloatTempVec[0], diffFloatTempVec[0]);
	cv::add(diffFloatTempVec[2], diffFloatTempVec[0], diffFloatTempVec[0]);
	cv::sqrt(diffFloatTempVec[0], diffFloatTempVec[0]);
	cv::addWeighted(multiplier(ROI1), 0.5, multiplier(ROI2), 0.5, 0.0, multiplierAve);
	cv::multiply(diffFloatTempVec[0], multiplierAve, *returnMat);

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffFloatTempVec[i].release();
	}
}

// DBSCANGPU FUNCTIONS
dbScanGPU::dbScanGPU()
{
}

dbScanGPU::~dbScanGPU()
{
	releaseSUMats();
}

// input mask outlines the area where clusters with >50% overlap will be counted
// returns a segmented mask composed of all major clusters which are counted
int dbScanGPU::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	cv::UMat inMatU = mat(bounds).getUMat(cv::ACCESS_READ);

	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	// set labels to unclassified
	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}
	(*labelsMat)(bounds).setTo(UNCLASSIFIED);

	cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

	int clusterID = 0;
	cv::Mat labels = (*labelsMat)(bounds).clone();

	auto tstart = std::chrono::high_resolution_clock::now();

	scan2(&labels, inMatU, point, &clusterID, toleranceValue, minCluster);

	auto tend = std::chrono::high_resolution_clock::now();
	*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

	labels.copyTo((*labelsMat)(bounds));
	labels.release();
	inMatU.release();

	return clusterID;
}

// actual dbScanGPU algorithm
void dbScanGPU::scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMatU.cols, labMatU.rows);
	cv::UMat pixelsU;

	// add smoothing
	cv::medianBlur(labMatU, pixelsU, 3);

	// get initial rect and umat
	cv::Rect windowB(1, 1, bounds.width, bounds.height);

	// normalise to lab vec
	std::vector<cv::UMat> diffFloatVecB(labChannels);
	CIEDE::normaliseLab(pixelsU, &diffFloatVecB, (float)USHRT_MAX);
	cv::UMat multiplierU = CIEDE::getMultiplier(diffFloatVecB[0], windowB);

	// create vector of neighbour points and locations
	int vecSize = bounds.width * bounds.height;
	if (neighbourVecXSU.size() == 0 || neighbourVecPointSUB.size() == 0 || neighbourVecXSU[0].cols * neighbourVecXSU[0].rows != vecSize || (neighbourVecPointSUB[0].cols - 2) * (neighbourVecPointSUB[0].rows - 2) != vecSize) {
		releaseSUMats();
		neighbourVecXSU.reserve(neighbourCount);
		neighbourVecYSU.reserve(neighbourCount);
		neighbourVecPointSUB.reserve(2);

		// initialise neighbourVecPoint with coordinates
		neighbourVecPointSUB.push_back(cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSUB.push_back(cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSUB[0].setTo(NONE);
		neighbourVecPointSUB[1].setTo(NONE);
		for (int x = 0; x < bounds.width; x++) {
			neighbourVecPointSUB[0].col(x + 1).setTo(x);
		}
		for (int y = 0; y < bounds.height; y++) {
			neighbourVecPointSUB[1].row(y + 1).setTo(y);
		}

		// initialise neighbourVecXU/Y with empty UMats
		for (int i = 0; i < neighbourCount; i++) {
			neighbourVecXSU.push_back(cv::UMat(bounds.height, bounds.width, CV_32SC1));
			neighbourVecYSU.push_back(cv::UMat(bounds.height, bounds.width, CV_32SC1));
		}
	}

	// iterate through each neighbour displacement
	cv::UMat compareU(bounds.height, bounds.width, CV_32FC1);
	compareU.setTo(toleranceValue);
	cv::UMat resultU(bounds.height, bounds.width, CV_8UC1);
	for (int i = 0; i < neighbourCount; i++) {
		cv::UMat neighbourDiffFloatU;
		cv::Rect ROI2(1 + neighbourLoc[i].x, 1 + neighbourLoc[i].y, bounds.width, bounds.height);
		CIEDE::compareNormalisedE76UMat(diffFloatVecB, multiplierU, windowB, ROI2, &neighbourDiffFloatU);

		// compare difference with the tolerance threshold and copy the point location to neighbourVecXU/Y if included
		// if point not included, then both X & Y values are set to NONE
		cv::compare(neighbourDiffFloatU, compareU, resultU, cv::CMP_LE);
		neighbourVecXSU[i].setTo(NONE);
		neighbourVecYSU[i].setTo(NONE);

		int dispX = neighbourLoc[i].x;
		int dispY = neighbourLoc[i].y;
		cv::Rect dispBounds(dispX + 1, dispY + 1, bounds.width, bounds.height);
		neighbourVecPointSUB[0](dispBounds).copyTo(neighbourVecXSU[i], resultU);
		neighbourVecPointSUB[1](dispBounds).copyTo(neighbourVecYSU[i], resultU);
		neighbourDiffFloatU.release();
	}

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffFloatVecB[i].release();
	}
	pixelsU.release();
	compareU.release();
	resultU.release();

	// create neighbour buffer and initialise
	indexSize = bounds.height * bounds.width;
	int neighbourVecSize = indexSize * sizeof(int);
	int* neighbourBuffer = (int*)malloc(neighbourCount * 2 * neighbourVecSize);
	int* labelsBuffer = (int*)malloc(neighbourVecSize);
	int* offsetBuffer = (int*)malloc(neighbourVecSize);

	if (neighbourBuffer != NULL && labelsBuffer != NULL && offsetBuffer != NULL) {
		for (int i = 0; i < neighbourCount; i++) {
			int indexX = i * indexSize;
			int indexY = indexX + (neighbourCount * indexSize);

			cv::Mat neighbourX = neighbourVecXSU[i].getMat(cv::ACCESS_READ);
			cv::Mat neighbourY = neighbourVecYSU[i].getMat(cv::ACCESS_READ);
			memcpy(neighbourBuffer + indexX, (int*)neighbourX.data, neighbourVecSize);
			memcpy(neighbourBuffer + indexY, (int*)neighbourY.data, neighbourVecSize);
			neighbourX.release();
			neighbourY.release();
		}

		// create atomic vector for labels
		std::vector<int> labelsVec = utility::matToVec<int>(*labels);
		memcpy(labelsBuffer, labelsVec.data(), neighbourVecSize);

		// start at the center of the rect, then run through the remainder
		*clusterID = 1;
		robin_hood::unordered_flat_map<int, int> clusterDict;
		int clusterCapacity = 0;

		expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &clusterDict, &clusterCapacity, clusterID);
		for (int y = 0; y < bounds.height; y++) {
			for (int x = 0; x < bounds.width; x++) {
				expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID);
			}
		}

		if (minCluster > 1) {
			// set small clusters to NOISE
			robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
			includedClusters.reserve(clusterDict.size());
			excludedClusters.reserve(clusterDict.size());
			for (int i = 1; i <= clusterDict.size(); i++) {
				if (clusterDict[i] >= minCluster) {
					includedClusters.emplace(i);
				}
				else {
					excludedClusters.emplace(i);
				}
			}

			std::vector<int> indexSizeVec(indexSize);
			std::iota(indexSizeVec.begin(), indexSizeVec.end(), 0);

			if (includedClusters.size() > excludedClusters.size()) {
				// use excluded clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &excludedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && excludedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}
			else {
				// use included clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &includedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && !includedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}

			// reduce the cluster size accordingly
			*clusterID -= (int)excludedClusters.size();
		}

		memcpy(labels->data, labelsBuffer, neighbourVecSize);
	}

	// clean up
	free(neighbourBuffer);
	free(labelsBuffer);
	free(offsetBuffer);
	multiplierU.release();
}

// expand clusters from a point
void dbScanGPU::expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID)
{
	if (*(labelsBuffer + (point.y * bounds.width) + point.x) == UNCLASSIFIED) {
		int count = 0;
		if (expandCluster2(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &count, *clusterID) == FAILURE) {
			*(labelsBuffer + (point.y * bounds.width) + point.x) = NOISE;
		}
		else {
			if (clusterDict->size() == *clusterCapacity) {
				*clusterCapacity += clusterIncrements;
				clusterDict->reserve(*clusterCapacity);
			}
			clusterDict->emplace(*clusterID, count);
			(*clusterID)++;
		}
	}
}

// expand clusters from a point
int dbScanGPU::expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID)
{
	int offsetStart = 0;
	int offsetEnd = 0;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, point, clusterID);

	if (offsetStart == offsetEnd) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		*(labelsBuffer + ((point.y * bounds.width) + point.x)) = clusterID;

		while (offsetStart < offsetEnd) {
			int intoffset2 = *(offsetBuffer + offsetStart);
			offsetStart++;
			calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, intoffset2, clusterID);
		}

		*count = offsetEnd;
		return SUCCESS;
	}
}

void dbScanGPU::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID)
{
	int intoffset = (point.y * bounds.width) + point.x;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, offsetEnd, bounds, intoffset, clusterID);
}

void dbScanGPU::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID)
{
	int bufferDifference = neighbourCount * indexSize;
	for (int i = 0; i < neighbourCount; i++) {
		int offset = (i * indexSize) + intoffset;
		int x = *(neighbourBuffer + offset);
		if (x != NONE) {
			int y = *(neighbourBuffer + offset + bufferDifference);
			int intoffset2 = (y * bounds.width) + x;
			int labelNeighbour = *(labelsBuffer + intoffset2);
			if (labelNeighbour == UNCLASSIFIED) {
				*(labelsBuffer + intoffset2) = clusterID;
				*(offsetBuffer + *offsetEnd) = intoffset2;
				(*offsetEnd)++;
			}
		}
	}
}

// release all umats
void dbScanGPU::releaseSUMats()
{
	std::vector<cv::UMat>::iterator itMatU;
	for (itMatU = neighbourVecXSU.begin(); itMatU != neighbourVecXSU.end(); ++itMatU) {
		itMatU->release();
	}
	for (itMatU = neighbourVecYSU.begin(); itMatU != neighbourVecYSU.end(); ++itMatU) {
		itMatU->release();
	}
	for (itMatU = neighbourVecPointSUB.begin(); itMatU != neighbourVecPointSUB.end(); ++itMatU) {
		itMatU->release();
	}

	neighbourVecXSU.clear();
	neighbourVecYSU.clear();
	neighbourVecPointSUB.clear();
}

// DBSCANGPUINIT FUNCTIONS
dbScanGPUInit::dbScanGPUInit(int maxwidth, int maxheight)
{
	dbScanGPUInit::maxwidth = maxwidth;
	dbScanGPUInit::maxheight = maxheight;
	neighbourVecPointSUBPreset[0] = cv::UMat(maxheight, maxwidth, CV_32SC1);
	neighbourVecPointSUBPreset[1] = cv::UMat(maxheight, maxwidth, CV_32SC1);

	// initialise neighbourVecPoint with coordinates
	for (int x = 0; x < maxwidth; x++) {
		neighbourVecPointSUBPreset[0].col(x).setTo(x);
	}
	for (int y = 0; y < maxheight; y++) {
		neighbourVecPointSUBPreset[1].row(y).setTo(y);
	}
}

dbScanGPUInit::~dbScanGPUInit()
{
	std::vector<cv::UMat>::iterator itMatU;
	for (itMatU = neighbourVecPointSUBPreset.begin(); itMatU != neighbourVecPointSUBPreset.end(); ++itMatU) {
		itMatU->release();
	}
	neighbourVecPointSUBPreset.clear();
}

// input mask outlines the area where clusters with >50% overlap will be counted
// returns a segmented mask composed of all major clusters which are counted
int dbScanGPUInit::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	if (boundswidth <= maxwidth && boundsheight <= maxheight) {
		cv::UMat inMatU = mat(bounds).getUMat(cv::ACCESS_READ);

		// set labels to unclassified
		if (labelsMat->empty()) {
			*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
		}
		else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
			labelsMat->release();
			*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
		}
		else {
			labelsMat->setTo(NONE);
		}
		(*labelsMat)(bounds).setTo(UNCLASSIFIED);

		cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

		int clusterID = 0;
		cv::Mat labels = (*labelsMat)(bounds).clone();

		auto tstart = std::chrono::high_resolution_clock::now();

		scan2(&labels, inMatU, point, &clusterID, toleranceValue, minCluster);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

		labels.copyTo((*labelsMat)(bounds));
		labels.release();
		inMatU.release();

		return clusterID;
	}
	else {
		*duration = 0;
		return 0;
	}
}

// actual dbScanGPU algorithm
void dbScanGPUInit::scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMatU.cols, labMatU.rows);
	cv::UMat pixelsU;

	// add smoothing
	cv::medianBlur(labMatU, pixelsU, 3);

	// get initial rect and umat
	cv::Rect windowB(1, 1, bounds.width, bounds.height);
	cv::Rect window(0, 0, bounds.width, bounds.height);

	// normalise to lab vec
	std::vector<cv::UMat> diffFloatVecB(labChannels);
	CIEDE::normaliseLab(pixelsU, &diffFloatVecB, (float)USHRT_MAX);
	cv::UMat multiplierU = CIEDE::getMultiplier(diffFloatVecB[0], windowB);

	// create vector of neighbour points and locations
	int vecSize = bounds.width * bounds.height;
	std::vector<cv::UMat> neighbourVecXSU = std::vector<cv::UMat>(neighbourCount);
	std::vector<cv::UMat> neighbourVecYSU = std::vector<cv::UMat>(neighbourCount);

	// initialise neighbourVecXU/Y with empty UMats
	for (int i = 0; i < neighbourCount; i++) {
		neighbourVecXSU[i] = cv::UMat(bounds.height, bounds.width, CV_32SC1);
		neighbourVecYSU[i] = cv::UMat(bounds.height, bounds.width, CV_32SC1);
	}

	// iterate through each neighbour displacement
	cv::UMat compareU(bounds.height, bounds.width, CV_32FC1);
	compareU.setTo(toleranceValue);
	cv::UMat resultU(bounds.height, bounds.width, CV_8UC1);

	std::vector<cv::UMat> neighbourVecPointSUB(2);
	neighbourVecPointSUB[0] = cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1);
	neighbourVecPointSUB[1] = cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1);
	neighbourVecPointSUB[0].row(0).setTo(NONE);
	neighbourVecPointSUB[0].row(neighbourVecPointSUB[0].rows - 1).setTo(NONE);
	neighbourVecPointSUB[0].col(0).setTo(NONE);
	neighbourVecPointSUB[0].col(neighbourVecPointSUB[0].cols - 1).setTo(NONE);
	neighbourVecPointSUBPreset[0](window).copyTo(neighbourVecPointSUB[0](windowB));
	neighbourVecPointSUBPreset[1](window).copyTo(neighbourVecPointSUB[1](windowB));

	for (int i = 0; i < neighbourCount; i++) {
		cv::UMat neighbourDiffFloatU;
		cv::Rect ROI2(1 + neighbourLoc[i].x, 1 + neighbourLoc[i].y, bounds.width, bounds.height);
		CIEDE::compareNormalisedE76UMat(diffFloatVecB, multiplierU, windowB, ROI2, &neighbourDiffFloatU);

		// compare difference with the tolerance threshold and copy the point location to neighbourVecXU/Y if included
		// if point not included, then both X & Y values are set to NONE
		cv::compare(neighbourDiffFloatU, compareU, resultU, cv::CMP_LE);
		neighbourVecXSU[i].setTo(NONE);
		neighbourVecYSU[i].setTo(NONE);

		int dispX = neighbourLoc[i].x;
		int dispY = neighbourLoc[i].y;
		cv::Rect dispBounds(dispX + 1, dispY + 1, bounds.width, bounds.height);
		neighbourVecPointSUB[0](dispBounds).copyTo(neighbourVecXSU[i], resultU);
		neighbourVecPointSUB[1](dispBounds).copyTo(neighbourVecYSU[i], resultU);
		neighbourDiffFloatU.release();
	}

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffFloatVecB[i].release();
	}
	pixelsU.release();
	compareU.release();
	resultU.release();
	neighbourVecPointSUB[0].release();
	neighbourVecPointSUB[1].release();

	// create neighbour buffer and initialise
	indexSize = bounds.height * bounds.width;
	int neighbourVecSize = indexSize * sizeof(int);
	int* neighbourBuffer = (int*)malloc(neighbourCount * 2 * neighbourVecSize);
	int* labelsBuffer = (int*)malloc(neighbourVecSize);
	int* offsetBuffer = (int*)malloc(neighbourVecSize);

	if (neighbourBuffer != NULL && labelsBuffer != NULL && offsetBuffer != NULL) {
		for (int i = 0; i < neighbourCount; i++) {
			int indexX = i * indexSize;
			int indexY = indexX + (neighbourCount * indexSize);

			cv::Mat neighbourX = neighbourVecXSU[i].getMat(cv::ACCESS_READ);
			cv::Mat neighbourY = neighbourVecYSU[i].getMat(cv::ACCESS_READ);
			memcpy(neighbourBuffer + indexX, (int*)neighbourX.data, neighbourVecSize);
			memcpy(neighbourBuffer + indexY, (int*)neighbourY.data, neighbourVecSize);
			neighbourX.release();
			neighbourY.release();
		}

		// create atomic vector for labels
		std::vector<int> labelsVec = utility::matToVec<int>(*labels);
		memcpy(labelsBuffer, labelsVec.data(), neighbourVecSize);

		// start at the center of the rect, then run through the remainder
		*clusterID = 1;
		robin_hood::unordered_flat_map<int, int> clusterDict;
		int clusterCapacity = 0;

		expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &clusterDict, &clusterCapacity, clusterID);
		for (int y = 0; y < bounds.height; y++) {
			for (int x = 0; x < bounds.width; x++) {
				expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID);
			}
		}

		if (minCluster > 1) {
			// set small clusters to NOISE
			robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
			includedClusters.reserve(clusterDict.size());
			excludedClusters.reserve(clusterDict.size());
			for (int i = 1; i <= clusterDict.size(); i++) {
				if (clusterDict[i] >= minCluster) {
					includedClusters.emplace(i);
				}
				else {
					excludedClusters.emplace(i);
				}
			}

			std::vector<int> indexSizeVec(indexSize);
			std::iota(indexSizeVec.begin(), indexSizeVec.end(), 0);

			if (includedClusters.size() > excludedClusters.size()) {
				// use excluded clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &excludedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && excludedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}
			else {
				// use included clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &includedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && !includedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}

			// reduce the cluster size accordingly
			*clusterID -= (int)excludedClusters.size();
		}

		memcpy(labels->data, labelsBuffer, neighbourVecSize);
	}

	// clean up
	free(neighbourBuffer);
	free(labelsBuffer);
	free(offsetBuffer);
	multiplierU.release();

	std::vector<cv::UMat>::iterator itMatU;
	for (itMatU = neighbourVecXSU.begin(); itMatU != neighbourVecXSU.end(); ++itMatU) {
		itMatU->release();
	}
	for (itMatU = neighbourVecYSU.begin(); itMatU != neighbourVecYSU.end(); ++itMatU) {
		itMatU->release();
	}
	neighbourVecXSU.clear();
	neighbourVecYSU.clear();
}

// expand clusters from a point
void dbScanGPUInit::expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID)
{
	if (cv::Rect(0, 0, bounds.width, bounds.height).contains(point)) {
		if (*(labelsBuffer + (point.y * bounds.width) + point.x) == UNCLASSIFIED) {
			int count = 0;
			if (expandCluster2(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &count, *clusterID) == FAILURE) {
				*(labelsBuffer + (point.y * bounds.width) + point.x) = NOISE;
			}
			else {
				if (clusterDict->size() == *clusterCapacity) {
					*clusterCapacity += clusterIncrements;
					clusterDict->reserve(*clusterCapacity);
				}
				clusterDict->emplace(*clusterID, count);
				(*clusterID)++;
			}
		}
	}
}

// expand clusters from a point
int dbScanGPUInit::expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID)
{
	int offsetStart = 0;
	int offsetEnd = 0;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, point, clusterID);

	if (offsetStart == offsetEnd) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		*(labelsBuffer + ((point.y * bounds.width) + point.x)) = clusterID;

		while (offsetStart < offsetEnd) {
			int intoffset2 = *(offsetBuffer + offsetStart);
			offsetStart++;
			calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, intoffset2, clusterID);
		}

		*count = offsetEnd;
		return SUCCESS;
	}
}

void dbScanGPUInit::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID)
{
	int intoffset = (point.y * bounds.width) + point.x;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, offsetEnd, bounds, intoffset, clusterID);
}

void dbScanGPUInit::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID)
{
	int bufferDifference = neighbourCount * indexSize;
	for (int i = 0; i < neighbourCount; i++) {
		int offset = (i * indexSize) + intoffset;
		int x = *(neighbourBuffer + offset);
		if (x != NONE) {
			int y = *(neighbourBuffer + offset + bufferDifference);
			int intoffset2 = (y * bounds.width) + x;
			int labelNeighbour = *(labelsBuffer + intoffset2);
			if (labelNeighbour == UNCLASSIFIED) {
				*(labelsBuffer + intoffset2) = clusterID;
				*(offsetBuffer + *offsetEnd) = intoffset2;
				(*offsetEnd)++;
			}
		}
	}
}

// DBSCANGPUPAR FUNCTIONS
dbScanGPUPar::dbScanGPUPar(unsigned concurrentthreads)
{
	dbScanGPUPar::concurrentthreads = concurrentthreads;
	bitMap = (BYTE*)malloc(concurrentthreads);
	if (bitMap != NULL) {
		BYTE map = 1;
		for (int i = 0; i < (int)concurrentthreads; i++) {
			*(bitMap + i) = map;
			map = map << 1;
		}
	}
}

dbScanGPUPar::dbScanGPUPar()
{
	dbScanGPUPar::concurrentthreads = 0;
	bitMap = NULL;
}

dbScanGPUPar::~dbScanGPUPar()
{
	releaseSUMats();
	free(bitMap);
}

// input mask outlines the area where clusters with >50% overlap will be counted
// returns a segmented mask composed of all major clusters which are counted
int dbScanGPUPar::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	cv::UMat inMatU = mat(bounds).getUMat(cv::ACCESS_READ);

	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	// set labels to unclassified
	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}
	(*labelsMat)(bounds).setTo(UNCLASSIFIED);

	cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

	int clusterID = 0;
	cv::Mat labels = (*labelsMat)(bounds).clone();

	auto tstart = std::chrono::high_resolution_clock::now();

	scan2(&labels, inMatU, point, &clusterID, toleranceValue, minCluster);

	auto tend = std::chrono::high_resolution_clock::now();
	*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

	labels.copyTo((*labelsMat)(bounds));
	labels.release();
	inMatU.release();

	return clusterID;
}

// actual dbScanGPU algorithm
void dbScanGPUPar::scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMatU.cols, labMatU.rows);
	cv::UMat pixelsU;

	// add smoothing
	cv::medianBlur(labMatU, pixelsU, 3);

	// get initial rect and umat
	cv::Rect windowB(1, 1, bounds.width, bounds.height);

	// normalise to lab vec
	std::vector<cv::UMat> diffFloatVecB(labChannels);
	CIEDE::normaliseLab(pixelsU, &diffFloatVecB, (float)USHRT_MAX);
	cv::UMat multiplierU = CIEDE::getMultiplier(diffFloatVecB[0], windowB);

	// create vector of neighbour points and locations
	int vecSize = bounds.width * bounds.height;
	if (neighbourVecXSU.size() == 0 || neighbourVecPointSUB.size() == 0 || neighbourVecXSU[0].cols * neighbourVecXSU[0].rows != vecSize || (neighbourVecPointSUB[0].cols - 2) * (neighbourVecPointSUB[0].rows - 2) != vecSize) {
		releaseSUMats();
		neighbourVecXSU.reserve(neighbourCount);
		neighbourVecYSU.reserve(neighbourCount);
		neighbourVecPointSUB.reserve(2);

		// initialise neighbourVecPoint with coordinates
		neighbourVecPointSUB.push_back(cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSUB.push_back(cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSUB[0].setTo(NONE);
		neighbourVecPointSUB[1].setTo(NONE);
		for (int x = 0; x < bounds.width; x++) {
			neighbourVecPointSUB[0].col(x + 1).setTo(x);
		}
		for (int y = 0; y < bounds.height; y++) {
			neighbourVecPointSUB[1].row(y + 1).setTo(y);
		}

		// initialise neighbourVecXU/Y with empty UMats
		for (int i = 0; i < neighbourCount; i++) {
			neighbourVecXSU.push_back(cv::UMat(bounds.height, bounds.width, CV_32SC1));
			neighbourVecYSU.push_back(cv::UMat(bounds.height, bounds.width, CV_32SC1));
		}
	}

	// iterate through each neighbour displacement
	cv::UMat compareU(bounds.height, bounds.width, CV_32FC1);
	compareU.setTo(toleranceValue);
	cv::UMat resultU(bounds.height, bounds.width, CV_8UC1);
	for (int i = 0; i < neighbourCount; i++) {
		cv::UMat neighbourDiffFloatU;
		cv::Rect ROI2(1 + neighbourLoc[i].x, 1 + neighbourLoc[i].y, bounds.width, bounds.height);
		CIEDE::compareNormalisedE76UMat(diffFloatVecB, multiplierU, windowB, ROI2, &neighbourDiffFloatU);

		// compare difference with the tolerance threshold and copy the point location to neighbourVecXU/Y if included
		// if point not included, then both X & Y values are set to NONE
		cv::compare(neighbourDiffFloatU, compareU, resultU, cv::CMP_LE);
		neighbourVecXSU[i].setTo(NONE);
		neighbourVecYSU[i].setTo(NONE);

		int dispX = neighbourLoc[i].x;
		int dispY = neighbourLoc[i].y;
		cv::Rect dispBounds(dispX + 1, dispY + 1, bounds.width, bounds.height);
		neighbourVecPointSUB[0](dispBounds).copyTo(neighbourVecXSU[i], resultU);
		neighbourVecPointSUB[1](dispBounds).copyTo(neighbourVecYSU[i], resultU);
		neighbourDiffFloatU.release();
	}

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffFloatVecB[i].release();
	}
	pixelsU.release();
	compareU.release();
	resultU.release();

	// create neighbour buffer and initialise
	indexSize = bounds.height * bounds.width;
	int neighbourVecSize = indexSize * sizeof(int);
	int* neighbourBuffer = (int*)malloc(neighbourCount * 2 * neighbourVecSize);
	int* labelsBuffer = (int*)malloc(neighbourVecSize);
	moodycamel::ConcurrentQueue<int> offsetQueue(neighbourVecSize);

	std::vector<int> indexCountVec(concurrentthreads);
	std::iota(indexCountVec.begin(), indexCountVec.end(), 0);

	if (neighbourBuffer != NULL && labelsBuffer != NULL) {
		for (int i = 0; i < neighbourCount; i++) {
			int indexX = i * indexSize;
			int indexY = indexX + (neighbourCount * indexSize);

			cv::Mat neighbourX = neighbourVecXSU[i].getMat(cv::ACCESS_READ);
			cv::Mat neighbourY = neighbourVecYSU[i].getMat(cv::ACCESS_READ);
			memcpy(neighbourBuffer + indexX, (int*)neighbourX.data, neighbourVecSize);
			memcpy(neighbourBuffer + indexY, (int*)neighbourY.data, neighbourVecSize);
			neighbourX.release();
			neighbourY.release();
		}

		// create atomic vector for labels
		std::vector<int> labelsVec = utility::matToVec<int>(*labels);
		memcpy(labelsBuffer, labelsVec.data(), neighbourVecSize);

		// start at the center of the rect, then run through the remainder
		*clusterID = 1;
		robin_hood::unordered_flat_map<int, int> clusterDict;
		int clusterCapacity = 0;

		expandCluster(neighbourBuffer, labelsBuffer, &offsetQueue, &indexCountVec, bounds, point, &clusterDict, &clusterCapacity, clusterID);
		for (int y = 0; y < bounds.height; y++) {
			for (int x = 0; x < bounds.width; x++) {
				expandCluster(neighbourBuffer, labelsBuffer, &offsetQueue, &indexCountVec, bounds, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID);
			}
		}

		if (minCluster > 1) {
			// set small clusters to NOISE
			robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
			includedClusters.reserve(clusterDict.size());
			excludedClusters.reserve(clusterDict.size());
			for (int i = 1; i <= clusterDict.size(); i++) {
				if (clusterDict[i] >= minCluster) {
					includedClusters.emplace(i);
				}
				else {
					excludedClusters.emplace(i);
				}
			}

			std::vector<int> indexSizeVec(indexSize);
			std::iota(indexSizeVec.begin(), indexSizeVec.end(), 0);

			if (includedClusters.size() > excludedClusters.size()) {
				// use excluded clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &excludedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && excludedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}
			else {
				// use included clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &includedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && !includedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}

			// reduce the cluster size accordingly
			*clusterID -= (int)excludedClusters.size();
		}

		memcpy(labels->data, labelsBuffer, neighbourVecSize);
	}

	// clean up
	free(neighbourBuffer);
	free(labelsBuffer);
	multiplierU.release();
}

// expand clusters from a point
void dbScanGPUPar::expandCluster(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const std::vector<int>* indexCountVec, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID)
{
	if (cv::Rect(0, 0, bounds.width, bounds.height).contains(point)) {
		if (*(labelsBuffer + (point.y * bounds.width) + point.x) == UNCLASSIFIED) {
			int count = 0;
			if (expandCluster2(neighbourBuffer, labelsBuffer, offsetQueue, indexCountVec, bounds, point, &count, *clusterID) == FAILURE) {
				*(labelsBuffer + (point.y * bounds.width) + point.x) = NOISE;
			}
			else {
				if (clusterDict->size() == *clusterCapacity) {
					*clusterCapacity += clusterIncrements;
					clusterDict->reserve(*clusterCapacity);
				}
				clusterDict->emplace(*clusterID, count);
				(*clusterID)++;
			}
		}
	}
}

// expand clusters from a point
int dbScanGPUPar::expandCluster2(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const std::vector<int>* indexCountVec, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID)
{
	std::atomic<int> currentCount;
	currentCount.store(calculateCluster(neighbourBuffer, labelsBuffer, offsetQueue, bounds, point, clusterID));

	if (currentCount.load() == 0) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		*(labelsBuffer + ((point.y * bounds.width) + point.x)) = clusterID;

		// count clusters
		std::for_each(std::execution::par_unseq, indexCountVec->begin(), indexCountVec->end(), [&](const int& v) {
			int intoffset2;
			while (offsetQueue->try_dequeue(intoffset2)) {
				currentCount.fetch_add(calculateCluster(neighbourBuffer, labelsBuffer, offsetQueue, bounds, intoffset2, clusterID));
			}
			});

		*count = currentCount.load();
		return SUCCESS;
	}
}

int dbScanGPUPar::calculateCluster(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const cv::Size& bounds, const cv::Point& point, int clusterID)
{
	int intoffset = (point.y * bounds.width) + point.x;
	return calculateCluster(neighbourBuffer, labelsBuffer, offsetQueue, bounds, intoffset, clusterID);
}

int dbScanGPUPar::calculateCluster(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const cv::Size& bounds, int intoffset, int clusterID)
{
	int count = 0;
	int bufferDifference = neighbourCount * indexSize;
	for (int i = 0; i < neighbourCount; i++) {
		int offset = (i * indexSize) + intoffset;
		int x = *(neighbourBuffer + offset);
		if (x != NONE) {
			int y = *(neighbourBuffer + offset + bufferDifference);
			int intoffset2 = (y * bounds.width) + x;
			int labelNeighbour = *(labelsBuffer + intoffset2);
			if (labelNeighbour == UNCLASSIFIED) {
				*(labelsBuffer + intoffset2) = clusterID;
				offsetQueue->enqueue(intoffset2);
				count++;
			}
		}
	}
	return count;
}

// release all umats
void dbScanGPUPar::releaseSUMats()
{
	std::vector<cv::UMat>::iterator itMatU;
	for (itMatU = neighbourVecXSU.begin(); itMatU != neighbourVecXSU.end(); ++itMatU) {
		itMatU->release();
	}
	for (itMatU = neighbourVecYSU.begin(); itMatU != neighbourVecYSU.end(); ++itMatU) {
		itMatU->release();
	}
	for (itMatU = neighbourVecPointSUB.begin(); itMatU != neighbourVecPointSUB.end(); ++itMatU) {
		itMatU->release();
	}

	neighbourVecXSU.clear();
	neighbourVecYSU.clear();
	neighbourVecPointSUB.clear();
}

// DBSCANCPU FUNCTIONS
dbScanCPU::dbScanCPU()
{
}

dbScanCPU::~dbScanCPU()
{
	releaseSMats();
}

// input mask outlines the area where clusters with >50% overlap will be counted
// returns a segmented mask composed of all major clusters which are counted
int dbScanCPU::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	cv::Mat inMat = mat(bounds);

	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	// set labels to unclassified
	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}
	(*labelsMat)(bounds).setTo(UNCLASSIFIED);

	cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

	int clusterID = 0;
	cv::Mat labels = (*labelsMat)(bounds).clone();

	auto tstart = std::chrono::high_resolution_clock::now();

	scan2(&labels, inMat, point, &clusterID, toleranceValue, minCluster);

	auto tend = std::chrono::high_resolution_clock::now();
	*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

	labels.copyTo((*labelsMat)(bounds));
	labels.release();
	inMat.release();

	return clusterID;
}

// actual dbScanCPU algorithm
void dbScanCPU::scan2(cv::Mat* labels, const cv::Mat& labMat, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMat.cols, labMat.rows);
	cv::Mat pixels;

	// add smoothing
	cv::medianBlur(labMat, pixels, 3);

	// get initial rect and umat
	cv::Rect windowB(1, 1, bounds.width, bounds.height);

	// normalise to lab vec
	std::vector<cv::Mat> diffFloatVecB(labChannels);
	CIEDE::normaliseLab(pixels, &diffFloatVecB, (float)USHRT_MAX);
	cv::Mat multiplier = CIEDE::getMultiplier(diffFloatVecB[0], windowB);

	// create vector of neighbour points and locations
	int vecSize = bounds.width * bounds.height;
	if (neighbourVecXS.size() == 0 || neighbourVecPointSB.size() == 0 || neighbourVecXS[0].cols * neighbourVecXS[0].rows != vecSize || (neighbourVecPointSB[0].cols - 2) * (neighbourVecPointSB[0].rows - 2) != vecSize) {
		releaseSMats();
		neighbourVecXS.reserve(neighbourCount);
		neighbourVecYS.reserve(neighbourCount);
		neighbourVecPointSB.reserve(2);

		// initialise neighbourVecPoint with coordinates
		neighbourVecPointSB.push_back(cv::Mat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSB.push_back(cv::Mat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSB[0].setTo(NONE);
		neighbourVecPointSB[1].setTo(NONE);
		for (int x = 0; x < bounds.width; x++) {
			neighbourVecPointSB[0].col(x + 1).setTo(x);
		}
		for (int y = 0; y < bounds.height; y++) {
			neighbourVecPointSB[1].row(y + 1).setTo(y);
		}

		// initialise neighbourVecX/Y with empty UMats
		for (int i = 0; i < neighbourCount; i++) {
			neighbourVecXS.push_back(cv::Mat(bounds.height, bounds.width, CV_32SC1));
			neighbourVecYS.push_back(cv::Mat(bounds.height, bounds.width, CV_32SC1));
		}
	}

	// iterate through each neighbour displacement
	cv::Mat compare(bounds.height, bounds.width, CV_32FC1);
	compare.setTo(toleranceValue);
	cv::Mat result(bounds.height, bounds.width, CV_8UC1);
	for (int i = 0; i < neighbourCount; i++) {
		cv::Mat neighbourDiffFloat;
		cv::Rect ROI2(1 + neighbourLoc[i].x, 1 + neighbourLoc[i].y, bounds.width, bounds.height);
		CIEDE::compareNormalisedE76UMat(diffFloatVecB, multiplier, windowB, ROI2, &neighbourDiffFloat);

		// compare difference with the tolerance threshold and copy the point location to neighbourVecXU/Y if included
		// if point not included, then both X & Y values are set to NONE
		cv::compare(neighbourDiffFloat, compare, result, cv::CMP_LE);
		neighbourVecXS[i].setTo(NONE);
		neighbourVecYS[i].setTo(NONE);

		int dispX = neighbourLoc[i].x;
		int dispY = neighbourLoc[i].y;
		cv::Rect dispBounds(dispX + 1, dispY + 1, bounds.width, bounds.height);
		neighbourVecPointSB[0](dispBounds).copyTo(neighbourVecXS[i], result);
		neighbourVecPointSB[1](dispBounds).copyTo(neighbourVecYS[i], result);
		neighbourDiffFloat.release();
	}

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffFloatVecB[i].release();
	}
	pixels.release();
	compare.release();
	result.release();

	// create neighbour buffer and initialise
	indexSize = bounds.height * bounds.width;
	int neighbourVecSize = indexSize * sizeof(int);
	int* neighbourBuffer = (int*)malloc(neighbourCount * 2 * neighbourVecSize);
	int* labelsBuffer = (int*)malloc(neighbourVecSize);
	int* offsetBuffer = (int*)malloc(neighbourVecSize);

	if (neighbourBuffer != NULL && labelsBuffer != NULL && offsetBuffer != NULL) {
		for (int i = 0; i < neighbourCount; i++) {
			int indexX = i * indexSize;
			int indexY = indexX + (neighbourCount * indexSize);

			memcpy(neighbourBuffer + indexX, (int*)neighbourVecXS[i].data, neighbourVecSize);
			memcpy(neighbourBuffer + indexY, (int*)neighbourVecYS[i].data, neighbourVecSize);
		}

		// create atomic vector for labels
		std::vector<int> labelsVec = utility::matToVec<int>(*labels);
		memcpy(labelsBuffer, labelsVec.data(), neighbourVecSize);

		// start at the center of the rect, then run through the remainder
		*clusterID = 1;
		robin_hood::unordered_flat_map<int, int> clusterDict;
		int clusterCapacity = 0;

		expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &clusterDict, &clusterCapacity, clusterID);
		for (int y = 0; y < bounds.height; y++) {
			for (int x = 0; x < bounds.width; x++) {
				expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID);
			}
		}

		if (minCluster > 1) {
			// set small clusters to NOISE
			robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
			includedClusters.reserve(clusterDict.size());
			excludedClusters.reserve(clusterDict.size());
			for (int i = 1; i <= clusterDict.size(); i++) {
				if (clusterDict[i] >= minCluster) {
					includedClusters.emplace(i);
				}
				else {
					excludedClusters.emplace(i);
				}
			}

			std::vector<int> indexSizeVec(indexSize);
			std::iota(indexSizeVec.begin(), indexSizeVec.end(), 0);

			if (includedClusters.size() > excludedClusters.size()) {
				// use excluded clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &excludedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && excludedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}
			else {
				// use included clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &includedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && !includedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}

			// reduce the cluster size accordingly
			*clusterID -= (int)excludedClusters.size();
		}

		memcpy(labels->data, labelsBuffer, neighbourVecSize);
	}

	// clean up
	free(neighbourBuffer);
	free(labelsBuffer);
	free(offsetBuffer);
	multiplier.release();
}

// expand clusters from a point
void dbScanCPU::expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID)
{
	if (cv::Rect(0, 0, bounds.width, bounds.height).contains(point)) {
		if (*(labelsBuffer + (point.y * bounds.width) + point.x) == UNCLASSIFIED) {
			int count = 0;
			if (expandCluster2(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &count, *clusterID) == FAILURE) {
				*(labelsBuffer + (point.y * bounds.width) + point.x) = NOISE;
			}
			else {
				if (clusterDict->size() == *clusterCapacity) {
					*clusterCapacity += clusterIncrements;
					clusterDict->reserve(*clusterCapacity);
				}
				clusterDict->emplace(*clusterID, count);
				(*clusterID)++;
			}
		}
	}
}

// expand clusters from a point
int dbScanCPU::expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID)
{
	int offsetStart = 0;
	int offsetEnd = 0;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, point, clusterID);

	if (offsetStart == offsetEnd) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		*(labelsBuffer + ((point.y * bounds.width) + point.x)) = clusterID;

		while (offsetStart < offsetEnd) {
			int intoffset2 = *(offsetBuffer + offsetStart);
			offsetStart++;
			calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, intoffset2, clusterID);
		}

		*count = offsetEnd;
		return SUCCESS;
	}
}

void dbScanCPU::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID)
{
	int intoffset = (point.y * bounds.width) + point.x;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, offsetEnd, bounds, intoffset, clusterID);
}

void dbScanCPU::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID)
{
	int bufferDifference = neighbourCount * indexSize;
	for (int i = 0; i < neighbourCount; i++) {
		int offset = (i * indexSize) + intoffset;
		int x = *(neighbourBuffer + offset);
		if (x != NONE) {
			int y = *(neighbourBuffer + offset + bufferDifference);
			int intoffset2 = (y * bounds.width) + x;
			int labelNeighbour = *(labelsBuffer + intoffset2);
			if (labelNeighbour == UNCLASSIFIED) {
				*(labelsBuffer + intoffset2) = clusterID;
				*(offsetBuffer + *offsetEnd) = intoffset2;
				(*offsetEnd)++;
			}
		}
	}
}

// release all mats
void dbScanCPU::releaseSMats()
{
	std::vector<cv::Mat>::iterator itMat;
	for (itMat = neighbourVecXS.begin(); itMat != neighbourVecXS.end(); ++itMat) {
		itMat->release();
	}
	for (itMat = neighbourVecYS.begin(); itMat != neighbourVecYS.end(); ++itMat) {
		itMat->release();
	}
	for (itMat = neighbourVecPointSB.begin(); itMat != neighbourVecPointSB.end(); ++itMat) {
		itMat->release();
	}

	neighbourVecXS.clear();
	neighbourVecYS.clear();
	neighbourVecPointSB.clear();
}

// DBSCANCPUINIT FUNCTIONS
dbScanCPUInit::dbScanCPUInit(int maxwidth, int maxheight)
{
	dbScanCPUInit::maxwidth = maxwidth;
	dbScanCPUInit::maxheight = maxheight;
	neighbourVecPointSBPreset[0] = cv::Mat(maxheight, maxwidth, CV_32SC1);
	neighbourVecPointSBPreset[1] = cv::Mat(maxheight, maxwidth, CV_32SC1);

	// initialise neighbourVecPoint with coordinates
	for (int x = 0; x < maxwidth; x++) {
		neighbourVecPointSBPreset[0].col(x).setTo(x);
	}
	for (int y = 0; y < maxheight; y++) {
		neighbourVecPointSBPreset[1].row(y).setTo(y);
	}
}

dbScanCPUInit::~dbScanCPUInit()
{
	std::vector<cv::Mat>::iterator itMat;
	for (itMat = neighbourVecPointSBPreset.begin(); itMat != neighbourVecPointSBPreset.end(); ++itMat) {
		itMat->release();
	}
	neighbourVecPointSBPreset.clear();
}

// input mask outlines the area where clusters with >50% overlap will be counted
// returns a segmented mask composed of all major clusters which are counted
int dbScanCPUInit::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	if (boundswidth <= maxwidth && boundsheight <= maxheight) {
		cv::Mat inMat = mat(bounds);

		// set labels to unclassified
		if (labelsMat->empty()) {
			*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
		}
		else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
			labelsMat->release();
			*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
		}
		else {
			labelsMat->setTo(NONE);
		}
		(*labelsMat)(bounds).setTo(UNCLASSIFIED);

		cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

		int clusterID = 0;
		cv::Mat labels = (*labelsMat)(bounds).clone();

		auto tstart = std::chrono::high_resolution_clock::now();

		scan2(&labels, inMat, point, &clusterID, toleranceValue, minCluster);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

		labels.copyTo((*labelsMat)(bounds));
		labels.release();
		inMat.release();

		return clusterID;
	}
	else {
		*duration = 0;
		return 0;
	}
}

// actual dbScanGPU algorithm
void dbScanCPUInit::scan2(cv::Mat* labels, const cv::Mat& labMat, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMat.cols, labMat.rows);
	cv::Mat pixels;

	// add smoothing
	cv::medianBlur(labMat, pixels, 3);

	// get initial rect and umat
	cv::Rect windowB(1, 1, bounds.width, bounds.height);
	cv::Rect window(0, 0, bounds.width, bounds.height);

	// normalise to lab vec
	std::vector<cv::Mat> diffFloatVecB(labChannels);
	CIEDE::normaliseLab(pixels, &diffFloatVecB, (float)USHRT_MAX);
	cv::Mat multiplier = CIEDE::getMultiplier(diffFloatVecB[0], windowB);

	// create vector of neighbour points and locations
	int vecSize = bounds.width * bounds.height;
	std::vector<cv::Mat> neighbourVecXS = std::vector<cv::Mat>(neighbourCount);
	std::vector<cv::Mat> neighbourVecYS = std::vector<cv::Mat>(neighbourCount);

	// initialise neighbourVecXU/Y with empty UMats
	for (int i = 0; i < neighbourCount; i++) {
		neighbourVecXS[i] = cv::Mat(bounds.height, bounds.width, CV_32SC1);
		neighbourVecYS[i] = cv::Mat(bounds.height, bounds.width, CV_32SC1);
	}

	// iterate through each neighbour displacement
	cv::Mat compare(bounds.height, bounds.width, CV_32FC1);
	compare.setTo(toleranceValue);
	cv::Mat result(bounds.height, bounds.width, CV_8UC1);

	std::vector<cv::Mat> neighbourVecPointSB(2);
	neighbourVecPointSB[0] = cv::Mat(bounds.height + 2, bounds.width + 2, CV_32SC1);
	neighbourVecPointSB[1] = cv::Mat(bounds.height + 2, bounds.width + 2, CV_32SC1);
	neighbourVecPointSB[0].row(0).setTo(NONE);
	neighbourVecPointSB[0].row(neighbourVecPointSB[0].rows - 1).setTo(NONE);
	neighbourVecPointSB[0].col(0).setTo(NONE);
	neighbourVecPointSB[0].col(neighbourVecPointSB[0].cols - 1).setTo(NONE);
	neighbourVecPointSBPreset[0](window).copyTo(neighbourVecPointSB[0](windowB));
	neighbourVecPointSBPreset[1](window).copyTo(neighbourVecPointSB[1](windowB));

	for (int i = 0; i < neighbourCount; i++) {
		cv::Mat neighbourDiffFloat;
		cv::Rect ROI2(1 + neighbourLoc[i].x, 1 + neighbourLoc[i].y, bounds.width, bounds.height);
		CIEDE::compareNormalisedE76UMat(diffFloatVecB, multiplier, windowB, ROI2, &neighbourDiffFloat);

		// compare difference with the tolerance threshold and copy the point location to neighbourVecXU/Y if included
		// if point not included, then both X & Y values are set to NONE
		cv::compare(neighbourDiffFloat, compare, result, cv::CMP_LE);
		neighbourVecXS[i].setTo(NONE);
		neighbourVecYS[i].setTo(NONE);

		int dispX = neighbourLoc[i].x;
		int dispY = neighbourLoc[i].y;
		cv::Rect dispBounds(dispX + 1, dispY + 1, bounds.width, bounds.height);
		neighbourVecPointSB[0](dispBounds).copyTo(neighbourVecXS[i], result);
		neighbourVecPointSB[1](dispBounds).copyTo(neighbourVecYS[i], result);
		neighbourDiffFloat.release();
	}

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffFloatVecB[i].release();
	}
	pixels.release();
	compare.release();
	result.release();
	neighbourVecPointSB[0].release();
	neighbourVecPointSB[1].release();

	// create neighbour buffer and initialise
	indexSize = bounds.height * bounds.width;
	int neighbourVecSize = indexSize * sizeof(int);
	int* neighbourBuffer = (int*)malloc(neighbourCount * 2 * neighbourVecSize);
	int* labelsBuffer = (int*)malloc(neighbourVecSize);
	int* offsetBuffer = (int*)malloc(neighbourVecSize);

	if (neighbourBuffer != NULL && labelsBuffer != NULL && offsetBuffer != NULL) {
		for (int i = 0; i < neighbourCount; i++) {
			int indexX = i * indexSize;
			int indexY = indexX + (neighbourCount * indexSize);

			memcpy(neighbourBuffer + indexX, (int*)neighbourVecXS[i].data, neighbourVecSize);
			memcpy(neighbourBuffer + indexY, (int*)neighbourVecYS[i].data, neighbourVecSize);
		}

		// create atomic vector for labels
		std::vector<int> labelsVec = utility::matToVec<int>(*labels);
		memcpy(labelsBuffer, labelsVec.data(), neighbourVecSize);

		// start at the center of the rect, then run through the remainder
		*clusterID = 1;
		robin_hood::unordered_flat_map<int, int> clusterDict;
		int clusterCapacity = 0;

		expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &clusterDict, &clusterCapacity, clusterID);
		for (int y = 0; y < bounds.height; y++) {
			for (int x = 0; x < bounds.width; x++) {
				expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID);
			}
		}

		if (minCluster > 1) {
			// set small clusters to NOISE
			robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
			includedClusters.reserve(clusterDict.size());
			excludedClusters.reserve(clusterDict.size());
			for (int i = 1; i <= clusterDict.size(); i++) {
				if (clusterDict[i] >= minCluster) {
					includedClusters.emplace(i);
				}
				else {
					excludedClusters.emplace(i);
				}
			}

			std::vector<int> indexSizeVec(indexSize);
			std::iota(indexSizeVec.begin(), indexSizeVec.end(), 0);

			if (includedClusters.size() > excludedClusters.size()) {
				// use excluded clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &excludedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && excludedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}
			else {
				// use included clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &includedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && !includedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}

			// reduce the cluster size accordingly
			*clusterID -= (int)excludedClusters.size();
		}

		memcpy(labels->data, labelsBuffer, neighbourVecSize);
	}

	// clean up
	free(neighbourBuffer);
	free(labelsBuffer);
	free(offsetBuffer);
	multiplier.release();

	std::vector<cv::Mat>::iterator itMat;
	for (itMat = neighbourVecXS.begin(); itMat != neighbourVecXS.end(); ++itMat) {
		itMat->release();
	}
	for (itMat = neighbourVecYS.begin(); itMat != neighbourVecYS.end(); ++itMat) {
		itMat->release();
	}
	neighbourVecXS.clear();
	neighbourVecYS.clear();
}

// expand clusters from a point
void dbScanCPUInit::expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID)
{
	if (cv::Rect(0, 0, bounds.width, bounds.height).contains(point)) {
		if (*(labelsBuffer + (point.y * bounds.width) + point.x) == UNCLASSIFIED) {
			int count = 0;
			if (expandCluster2(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &count, *clusterID) == FAILURE) {
				*(labelsBuffer + (point.y * bounds.width) + point.x) = NOISE;
			}
			else {
				if (clusterDict->size() == *clusterCapacity) {
					*clusterCapacity += clusterIncrements;
					clusterDict->reserve(*clusterCapacity);
				}
				clusterDict->emplace(*clusterID, count);
				(*clusterID)++;
			}
		}
	}
}

// expand clusters from a point
int dbScanCPUInit::expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID)
{
	int offsetStart = 0;
	int offsetEnd = 0;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, point, clusterID);

	if (offsetStart == offsetEnd) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		*(labelsBuffer + ((point.y * bounds.width) + point.x)) = clusterID;

		while (offsetStart < offsetEnd) {
			int intoffset2 = *(offsetBuffer + offsetStart);
			offsetStart++;
			calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, intoffset2, clusterID);
		}

		*count = offsetEnd;
		return SUCCESS;
	}
}

void dbScanCPUInit::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID)
{
	int intoffset = (point.y * bounds.width) + point.x;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, offsetEnd, bounds, intoffset, clusterID);
}

void dbScanCPUInit::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID)
{
	int bufferDifference = neighbourCount * indexSize;
	for (int i = 0; i < neighbourCount; i++) {
		int offset = (i * indexSize) + intoffset;
		int x = *(neighbourBuffer + offset);
		if (x != NONE) {
			int y = *(neighbourBuffer + offset + bufferDifference);
			int intoffset2 = (y * bounds.width) + x;
			int labelNeighbour = *(labelsBuffer + intoffset2);
			if (labelNeighbour == UNCLASSIFIED) {
				*(labelsBuffer + intoffset2) = clusterID;
				*(offsetBuffer + *offsetEnd) = intoffset2;
				(*offsetEnd)++;
			}
		}
	}
}

// DBSCANORIGINAL FUNCTIONS
dbScanOriginal::dbScanOriginal()
{
}

dbScanOriginal::~dbScanOriginal()
{
}

int dbScanOriginal::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	cv::Mat inMat = mat(bounds).clone();

	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	// set labels to unclassified
	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}
	(*labelsMat)(bounds).setTo(UNCLASSIFIED);

	cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

	int clusterID = 0;
	cv::Mat labels = (*labelsMat)(bounds).clone();

	auto tstart = std::chrono::high_resolution_clock::now();

	scan2(&labels, inMat, point, &clusterID, toleranceValue, minCluster);

	auto tend = std::chrono::high_resolution_clock::now();
	*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

	labels.copyTo((*labelsMat)(bounds));
	labels.release();
	inMat.release();

	return clusterID;
}

// returns a segmented mask
void dbScanOriginal::scan2(cv::Mat* labels, const cv::Mat& labMat, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMat.cols, labMat.rows);
	cv::Mat pixels;

	// add smoothing
	cv::medianBlur(labMat, pixels, 3);

	// get mean luminance
	cv::UMat diffFloat;
	std::vector<cv::Mat> diffVec;
	cv::split(pixels, diffVec);

	diffVec[0].convertTo(diffFloat, CV_32F);

	for (int i = 0; i < labChannels; i++) {
		diffVec[i].release();
	}

	// convert to L 0 to 100
	cv::multiply(diffFloat, cv::Scalar(100.0f / 255.0f), diffFloat);
	cv::min(diffFloat, cv::Scalar(100.0f), diffFloat);

	cv::UMat multiplierU = CIEDE::getMultiplier(diffFloat);
	cv::Mat multiplier = multiplierU.getMat(cv::ACCESS_READ);

	diffFloat.release();

	// start at the center of the rect, then run through the remainder
	*clusterID = 1;
	robin_hood::unordered_flat_map<int, int> clusterDict;
	int clusterCapacity = 0;

	expandCluster(labels, pixels, multiplier, point, &clusterDict, &clusterCapacity, clusterID, toleranceValue);
	for (int y = 0; y < bounds.height; y++) {
		for (int x = 0; x < bounds.width; x++) {
			expandCluster(labels, pixels, multiplier, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID, toleranceValue);
		}
	}

	if (minCluster > 1) {
		// set small clusters to NOISE
		robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
		includedClusters.reserve(clusterDict.size());
		excludedClusters.reserve(clusterDict.size());
		for (int i = 1; i <= clusterDict.size(); i++) {
			if (clusterDict[i] >= minCluster) {
				includedClusters.emplace(i);
			}
			else {
				excludedClusters.emplace(i);
			}
		}

		if (includedClusters.size() > excludedClusters.size()) {
			// use excluded clusters
			for (int y = 0; y < labels->rows; y++) {
				for (int x = 0; x < labels->cols; x++) {
					int label = labels->at<int>(y, x);
					if (label >= 1 && excludedClusters.contains(label)) {
						labels->at<int>(y, x) = NOISE;
					}
				}
			}
		}
		else {
			// use included clusters
			for (int y = 0; y < labels->rows; y++) {
				for (int x = 0; x < labels->cols; x++) {
					int label = labels->at<int>(y, x);
					if (label >= 1 && !includedClusters.contains(label)) {
						labels->at<int>(y, x) = NOISE;
					}
				}
			}
		}

		// reduce the cluster size accordingly
		*clusterID -= (int)excludedClusters.size();
	}

	// clean up
	multiplier.release();
	multiplierU.release();
	pixels.release();
}

// expand clusters from a point
void dbScanOriginal::expandCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID, float toleranceValue)
{
	if (labels->at<int>(point.y, point.x) == UNCLASSIFIED) {
		int count = 0;
		if (expandCluster2(labels, pixels, multiplier, point, &count, *clusterID, toleranceValue) == FAILURE) {
			labels->at<int>(point.y, point.x) = NOISE;
		}
		else {
			if (clusterDict->size() == *clusterCapacity) {
				*clusterCapacity += clusterIncrements;
				clusterDict->reserve(*clusterCapacity);
			}
			clusterDict->emplace(*clusterID, count);
			(*clusterID)++;
		}
	}
}

// expand clusters from a point
int dbScanOriginal::expandCluster2(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, int* count, int clusterID, float toleranceValue)
{
	std::vector<cv::Point> clusterSeeds = calculateCluster(labels, pixels, multiplier, point, toleranceValue);

	if (clusterSeeds.size() == 0) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		std::vector<cv::Point>::iterator itSeeds;
		for (itSeeds = clusterSeeds.begin(); itSeeds != clusterSeeds.end(); ++itSeeds) {
			labels->at<int>(itSeeds->y, itSeeds->x) = clusterID;
		}
		labels->at<int>(point.y, point.x) = clusterID;

		for (std::vector<int>::size_type i = 0, n = clusterSeeds.size(); i < n; ++i) {
			std::vector<cv::Point> clusterNeighbours = calculateCluster(labels, pixels, multiplier, clusterSeeds[i], toleranceValue);

			if (clusterNeighbours.size() > 0) {
				std::vector<cv::Point>::iterator it;
				for (it = clusterNeighbours.begin(); it != clusterNeighbours.end(); ++it) {
					int labelNeighbour = labels->at<int>(it->y, it->x);
					if (labelNeighbour == UNCLASSIFIED) {
						clusterSeeds.push_back(*it);
						n = clusterSeeds.size();
					}
					labels->at<int>(it->y, it->x) = clusterID;
				}
			}
		}

		*count = (int)clusterSeeds.size();
		return SUCCESS;
	}
}

// returns only unclassified and noise neighbours within the colour tolerance
std::vector<cv::Point> dbScanOriginal::calculateCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, float toleranceValue)
{
	std::vector<cv::Point> clusterVec;
	std::vector<cv::Point> neighbours = getNeighbours(cv::Rect(0, 0, pixels.cols, pixels.rows), point);
	cv::Vec3b pointLab = pixels.at<cv::Vec3b>(point.y, point.x);
	float pointMul = multiplier.at<float>(point.y, point.x);

	std::vector<cv::Point>::iterator it;
	for (it = neighbours.begin(); it != neighbours.end(); ++it) {
		int labelNeighbour = labels->at<int>(it->y, it->x);
		if (labelNeighbour == UNCLASSIFIED) {
			cv::Vec3b neighbourLab = pixels.at<cv::Vec3b>(it->y, it->x);
			float tolerance = CIEDE::getE76Byte(pointLab, neighbourLab) * (pointMul + multiplier.at<float>(it->y, it->x)) / 2.0f;
			if (tolerance <= toleranceValue) {
				clusterVec.push_back(*it);
			}
		}
	}
	return clusterVec;
}

// gets the neighbouring pixel locations
std::vector<cv::Point> dbScanOriginal::getNeighbours(const cv::Rect& bounds, const cv::Point& point)
{
	// iterate through immediate neighbours
	std::vector<cv::Point> neighbours;
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			if (y != 0 || x != 0) {
				cv::Point neighbour(point.x + x, point.y + y);
				if (bounds.contains(neighbour)) {
					neighbours.push_back(neighbour);
				}
			}
		}
	}
	return neighbours;
}

// DBSCANCIEDE2000 FUNCTIONS
dbScanCIEDE2000::dbScanCIEDE2000()
{
}

dbScanCIEDE2000::~dbScanCIEDE2000()
{
}

int dbScanCIEDE2000::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	cv::Mat inMat = mat(bounds).clone();

	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	// set labels to unclassified
	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}
	(*labelsMat)(bounds).setTo(UNCLASSIFIED);

	cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

	int clusterID = 0;
	cv::Mat labels = (*labelsMat)(bounds).clone();

	auto tstart = std::chrono::high_resolution_clock::now();

	scan2(&labels, inMat, point, &clusterID, toleranceValue, minCluster);

	auto tend = std::chrono::high_resolution_clock::now();
	*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

	labels.copyTo((*labelsMat)(bounds));
	labels.release();
	inMat.release();

	return clusterID;
}

// returns a segmented mask
void dbScanCIEDE2000::scan2(cv::Mat* labels, const cv::Mat& labMat, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMat.cols, labMat.rows);
	cv::Mat pixels;

	// add smoothing
	cv::medianBlur(labMat, pixels, 3);

	// get mean luminance
	cv::UMat diffFloat;
	std::vector<cv::Mat> diffVec;
	cv::split(pixels, diffVec);

	diffVec[0].convertTo(diffFloat, CV_32F);

	for (int i = 0; i < labChannels; i++) {
		diffVec[i].release();
	}

	// convert to L 0 to 100
	cv::multiply(diffFloat, cv::Scalar(100.0f / 255.0f), diffFloat);
	cv::min(diffFloat, cv::Scalar(100.0f), diffFloat);

	cv::UMat multiplierU = CIEDE::getMultiplier(diffFloat);
	cv::Mat multiplier = multiplierU.getMat(cv::ACCESS_READ);

	diffFloat.release();

	// start at the center of the rect, then run through the remainder
	*clusterID = 1;
	robin_hood::unordered_flat_map<int, int> clusterDict;
	int clusterCapacity = 0;

	expandCluster(labels, pixels, multiplier, point, &clusterDict, &clusterCapacity, clusterID, toleranceValue);
	for (int y = 0; y < bounds.height; y++) {
		for (int x = 0; x < bounds.width; x++) {
			expandCluster(labels, pixels, multiplier, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID, toleranceValue);
		}
	}

	if (minCluster > 1) {
		// set small clusters to NOISE
		robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
		includedClusters.reserve(clusterDict.size());
		excludedClusters.reserve(clusterDict.size());
		for (int i = 1; i <= clusterDict.size(); i++) {
			if (clusterDict[i] >= minCluster) {
				includedClusters.emplace(i);
			}
			else {
				excludedClusters.emplace(i);
			}
		}

		int indexSize = bounds.height * bounds.width;
		std::vector<int> indexSizeVec(indexSize);
		std::iota(indexSizeVec.begin(), indexSizeVec.end(), 0);

		if (includedClusters.size() > excludedClusters.size()) {
			// use excluded clusters
			for (int y = 0; y < labels->rows; y++) {
				for (int x = 0; x < labels->cols; x++) {
					int label = labels->at<int>(y, x);
					if (label >= 1 && excludedClusters.contains(label)) {
						labels->at<int>(y, x) = NOISE;
					}
				}
			}
		}
		else {
			// use included clusters
			for (int y = 0; y < labels->rows; y++) {
				for (int x = 0; x < labels->cols; x++) {
					int label = labels->at<int>(y, x);
					if (label >= 1 && !includedClusters.contains(label)) {
						labels->at<int>(y, x) = NOISE;
					}
				}
			}
		}

		// reduce the cluster size accordingly
		*clusterID -= (int)excludedClusters.size();
	}

	// clean up
	multiplier.release();
	multiplierU.release();
	pixels.release();
}

// expand clusters from a point
void dbScanCIEDE2000::expandCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID, float toleranceValue)
{
	if (labels->at<int>(point.y, point.x) == UNCLASSIFIED) {
		int count = 0;
		if (expandCluster2(labels, pixels, multiplier, point, &count, *clusterID, toleranceValue) == FAILURE) {
			labels->at<int>(point.y, point.x) = NOISE;
		}
		else {
			if (clusterDict->size() == *clusterCapacity) {
				*clusterCapacity += clusterIncrements;
				clusterDict->reserve(*clusterCapacity);
			}
			clusterDict->emplace(*clusterID, count);
			(*clusterID)++;
		}
	}
}

// expand clusters from a point
int dbScanCIEDE2000::expandCluster2(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, int* count, int clusterID, float toleranceValue)
{
	std::vector<cv::Point> clusterSeeds = calculateCluster(labels, pixels, multiplier, point, toleranceValue);

	if (clusterSeeds.size() == 0) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		std::vector<cv::Point>::iterator itSeeds;
		for (itSeeds = clusterSeeds.begin(); itSeeds != clusterSeeds.end(); ++itSeeds) {
			labels->at<int>(itSeeds->y, itSeeds->x) = clusterID;
		}
		labels->at<int>(point.y, point.x) = clusterID;

		for (std::vector<int>::size_type i = 0, n = clusterSeeds.size(); i < n; ++i) {
			std::vector<cv::Point> clusterNeighbours = calculateCluster(labels, pixels, multiplier, clusterSeeds[i], toleranceValue);

			if (clusterNeighbours.size() > 0) {
				std::vector<cv::Point>::iterator it;
				for (it = clusterNeighbours.begin(); it != clusterNeighbours.end(); ++it) {
					int labelNeighbour = labels->at<int>(it->y, it->x);
					if (labelNeighbour == UNCLASSIFIED) {
						clusterSeeds.push_back(*it);
						n = clusterSeeds.size();
					}
					labels->at<int>(it->y, it->x) = clusterID;
				}
			}
		}

		*count = (int)clusterSeeds.size();
		return SUCCESS;
	}
}

// returns only unclassified and noise neighbours within the colour tolerance
std::vector<cv::Point> dbScanCIEDE2000::calculateCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, float toleranceValue)
{
	std::vector<cv::Point> clusterVec;
	std::vector<cv::Point> neighbours = getNeighbours(cv::Rect(0, 0, pixels.cols, pixels.rows), point);
	cv::Vec3b pointLab = pixels.at<cv::Vec3b>(point.y, point.x);
	float pointMul = multiplier.at<float>(point.y, point.x);

	std::vector<cv::Point>::iterator it;
	for (it = neighbours.begin(); it != neighbours.end(); ++it) {
		int labelNeighbour = labels->at<int>(it->y, it->x);
		if (labelNeighbour == UNCLASSIFIED) {
			cv::Vec3b neighbourLab = pixels.at<cv::Vec3b>(it->y, it->x);
			float tolerance = CIEDE::getCIEDE2000(pointLab, neighbourLab) * (pointMul + multiplier.at<float>(it->y, it->x)) / 2.0f;
			if (tolerance <= toleranceValue) {
				clusterVec.push_back(*it);
			}
		}
	}
	return clusterVec;
}

// gets the neighbouring pixel locations
std::vector<cv::Point> dbScanCIEDE2000::getNeighbours(const cv::Rect& bounds, const cv::Point& point)
{
	// iterate through immediate neighbours
	std::vector<cv::Point> neighbours;
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			if (y != 0 || x != 0) {
				cv::Point neighbour(point.x + x, point.y + y);
				if (bounds.contains(neighbour)) {
					neighbours.push_back(neighbour);
				}
			}
		}
	}
	return neighbours;
}

// DBSCANNOISE FUNCTIONS
// precalculates noise pixels first and updates the labels
dbScanGPUNoise::dbScanGPUNoise()
{
}

dbScanGPUNoise::~dbScanGPUNoise()
{
	releaseSUMats();
}

// input mask outlines the area where clusters with >50% overlap will be counted
// returns a segmented mask composed of all major clusters which are counted
int dbScanGPUNoise::scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration)
{
	cv::UMat inMatU = mat(bounds).getUMat(cv::ACCESS_READ);

	int boundswidth = bounds.width;
	int boundsheight = bounds.height;

	// set labels to unclassified
	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != mat.rows || labelsMat->cols != mat.cols || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(mat.rows, mat.cols, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}
	(*labelsMat)(bounds).setTo(UNCLASSIFIED);

	cv::Point point((int)((float)boundswidth / 2.0f), (int)((float)boundsheight / 2.0f));

	int clusterID = 0;
	cv::Mat labels = (*labelsMat)(bounds).clone();

	auto tstart = std::chrono::high_resolution_clock::now();

	scan2(&labels, inMatU, point, &clusterID, toleranceValue, minCluster);

	auto tend = std::chrono::high_resolution_clock::now();
	*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();

	labels.copyTo((*labelsMat)(bounds));
	labels.release();
	inMatU.release();

	return clusterID;
}

// actual dbScanGPUNoise algorithm
void dbScanGPUNoise::scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster)
{
	cv::Size bounds(labMatU.cols, labMatU.rows);
	cv::UMat pixelsU;

	// add smoothing
	cv::medianBlur(labMatU, pixelsU, 3);

	// get initial rect and umat
	cv::Rect windowB(1, 1, bounds.width, bounds.height);

	// normalise to lab vec
	std::vector<cv::UMat> diffFloatVecB(labChannels);
	CIEDE::normaliseLab(pixelsU, &diffFloatVecB, (float)USHRT_MAX);
	cv::UMat multiplierU = CIEDE::getMultiplier(diffFloatVecB[0], windowB);

	// create vector of neighbour points and locations
	int vecSize = bounds.width * bounds.height;
	if (neighbourVecXSU.size() == 0 || neighbourVecPointSUB.size() == 0 || neighbourVecXSU[0].cols * neighbourVecXSU[0].rows != vecSize || (neighbourVecPointSUB[0].cols - 2) * (neighbourVecPointSUB[0].rows - 2) != vecSize) {
		releaseSUMats();
		neighbourVecXSU.reserve(neighbourCount);
		neighbourVecYSU.reserve(neighbourCount);
		neighbourVecPointSUB.reserve(2);

		// initialise neighbourVecPoint with coordinates
		neighbourVecPointSUB.push_back(cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSUB.push_back(cv::UMat(bounds.height + 2, bounds.width + 2, CV_32SC1));
		neighbourVecPointSUB[0].setTo(NONE);
		neighbourVecPointSUB[1].setTo(NONE);
		for (int x = 0; x < bounds.width; x++) {
			neighbourVecPointSUB[0].col(x + 1).setTo(x);
		}
		for (int y = 0; y < bounds.height; y++) {
			neighbourVecPointSUB[1].row(y + 1).setTo(y);
		}

		// initialise neighbourVecXU/Y with empty UMats
		for (int i = 0; i < neighbourCount; i++) {
			neighbourVecXSU.push_back(cv::UMat(bounds.height, bounds.width, CV_32SC1));
			neighbourVecYSU.push_back(cv::UMat(bounds.height, bounds.width, CV_32SC1));
		}
	}

	// iterate through each neighbour displacement
	cv::UMat compareU(bounds.height, bounds.width, CV_32FC1);
	compareU.setTo(toleranceValue);
	cv::UMat resultU(bounds.height, bounds.width, CV_8UC1);
	for (int i = 0; i < neighbourCount; i++) {
		cv::UMat neighbourDiffFloatU;
		cv::Rect ROI2(1 + neighbourLoc[i].x, 1 + neighbourLoc[i].y, bounds.width, bounds.height);
		CIEDE::compareNormalisedE76UMat(diffFloatVecB, multiplierU, windowB, ROI2, &neighbourDiffFloatU);

		// compare difference with the tolerance threshold and copy the point location to neighbourVecXU/Y if included
		// if point not included, then both X & Y values are set to NONE
		cv::compare(neighbourDiffFloatU, compareU, resultU, cv::CMP_LE);
		neighbourVecXSU[i].setTo(NONE);
		neighbourVecYSU[i].setTo(NONE);

		int dispX = neighbourLoc[i].x;
		int dispY = neighbourLoc[i].y;
		cv::Rect dispBounds(dispX + 1, dispY + 1, bounds.width, bounds.height);
		neighbourVecPointSUB[0](dispBounds).copyTo(neighbourVecXSU[i], resultU);
		neighbourVecPointSUB[1](dispBounds).copyTo(neighbourVecYSU[i], resultU);
		neighbourDiffFloatU.release();
	}

	// clean up
	for (int i = 0; i < labChannels; i++) {
		diffFloatVecB[i].release();
	}
	pixelsU.release();
	compareU.release();
	resultU.release();

	// create neighbour buffer and initialise
	indexSize = bounds.height * bounds.width;
	int neighbourVecSize = indexSize * sizeof(int);
	int* neighbourBuffer = (int*)malloc(neighbourCount * 2 * neighbourVecSize);
	int* labelsBuffer = (int*)malloc(neighbourVecSize);
	int* offsetBuffer = (int*)malloc(neighbourVecSize);

	if (neighbourBuffer != NULL && labelsBuffer != NULL && offsetBuffer != NULL) {
		cv::UMat noiseU = cv::UMat(bounds.height, bounds.width, CV_8UC1);
		noiseU.setTo(255);
		cv::UMat noiseMaskU;

		for (int i = 0; i < neighbourCount; i++) {
			int indexX = i * indexSize;
			int indexY = indexX + (neighbourCount * indexSize);

			// progressively remove non-noise pixels from noise mat
			cv::compare(neighbourVecXSU[i], NONE, noiseMaskU, cv::CMP_EQ);
			cv::bitwise_and(noiseU, noiseMaskU, noiseU);

			cv::Mat neighbourX = neighbourVecXSU[i].getMat(cv::ACCESS_READ);
			cv::Mat neighbourY = neighbourVecYSU[i].getMat(cv::ACCESS_READ);
			memcpy(neighbourBuffer + indexX, (int*)neighbourX.data, neighbourVecSize);
			memcpy(neighbourBuffer + indexY, (int*)neighbourY.data, neighbourVecSize);
			neighbourX.release();
			neighbourY.release();
		}

		// set noise pixels
		(*labels).setTo(NOISE, noiseU);
		noiseU.release();
		noiseMaskU.release();

		// create atomic vector for labels
		std::vector<int> labelsVec = utility::matToVec<int>(*labels);
		memcpy(labelsBuffer, labelsVec.data(), neighbourVecSize);

		// start at the center of the rect, then run through the remainder
		*clusterID = 1;
		robin_hood::unordered_flat_map<int, int> clusterDict;
		int clusterCapacity = 0;

		expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &clusterDict, &clusterCapacity, clusterID);
		for (int y = 0; y < bounds.height; y++) {
			for (int x = 0; x < bounds.width; x++) {
				expandCluster(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, cv::Point(x, y), &clusterDict, &clusterCapacity, clusterID);
			}
		}

		memcpy(labels->data, labelsBuffer, neighbourVecSize);

		if (minCluster > 1) {
			// set small clusters to NOISE
			robin_hood::unordered_flat_set<int> includedClusters, excludedClusters;
			includedClusters.reserve(clusterDict.size());
			excludedClusters.reserve(clusterDict.size());
			for (int i = 1; i <= clusterDict.size(); i++) {
				if (clusterDict[i] >= minCluster) {
					includedClusters.emplace(i);
				}
				else {
					excludedClusters.emplace(i);
				}
			}

			std::vector<int> indexSizeVec(indexSize);
			std::iota(indexSizeVec.begin(), indexSizeVec.end(), 0);

			if (includedClusters.size() > excludedClusters.size()) {
				// use excluded clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &excludedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && excludedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}
			else {
				// use included clusters
				std::for_each(std::execution::seq, indexSizeVec.begin(), indexSizeVec.end(), [&labelsBuffer, &includedClusters](int& v) {
					int label = *(labelsBuffer + v);
					if (label >= 1 && !includedClusters.contains(label)) {
						*(labelsBuffer + v) = NOISE;
					}
					});
			}

			// reduce the cluster size accordingly
			*clusterID -= (int)excludedClusters.size();
		}

		memcpy(labels->data, labelsBuffer, neighbourVecSize);
	}

	// clean up
	free(neighbourBuffer);
	free(labelsBuffer);
	free(offsetBuffer);
	multiplierU.release();
}

// expand clusters from a point
void dbScanGPUNoise::expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID)
{
	if (cv::Rect(0, 0, bounds.width, bounds.height).contains(point)) {
		if (*(labelsBuffer + (point.y * bounds.width) + point.x) == UNCLASSIFIED) {
			int count = 0;
			if (expandCluster2(neighbourBuffer, labelsBuffer, offsetBuffer, bounds, point, &count, *clusterID) == FAILURE) {
				*(labelsBuffer + (point.y * bounds.width) + point.x) = NOISE;
			}
			else {
				if (clusterDict->size() == *clusterCapacity) {
					*clusterCapacity += clusterIncrements;
					clusterDict->reserve(*clusterCapacity);
				}
				clusterDict->emplace(*clusterID, count);
				(*clusterID)++;
			}
		}
	}
}

// expand clusters from a point
int dbScanGPUNoise::expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID)
{
	int offsetStart = 0;
	int offsetEnd = 0;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, point, clusterID);

	if (offsetStart == offsetEnd) {
		*count = 0;
		return FAILURE;
	}
	else {
		// set cluster id and get core point index
		*(labelsBuffer + ((point.y * bounds.width) + point.x)) = clusterID;

		while (offsetStart < offsetEnd) {
			int intoffset2 = *(offsetBuffer + offsetStart);
			offsetStart++;
			calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, &offsetEnd, bounds, intoffset2, clusterID);
		}

		*count = offsetEnd;
		return SUCCESS;
	}
}

void dbScanGPUNoise::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID)
{
	int intoffset = (point.y * bounds.width) + point.x;
	calculateCluster(neighbourBuffer, labelsBuffer, offsetBuffer, offsetEnd, bounds, intoffset, clusterID);
}

void dbScanGPUNoise::calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID)
{
	int bufferDifference = neighbourCount * indexSize;
	for (int i = 0; i < neighbourCount; i++) {
		int offset = (i * indexSize) + intoffset;
		int x = *(neighbourBuffer + offset);
		if (x != NONE) {
			int y = *(neighbourBuffer + offset + bufferDifference);
			int intoffset2 = (y * bounds.width) + x;
			int labelNeighbour = *(labelsBuffer + intoffset2);
			if (labelNeighbour == UNCLASSIFIED) {
				*(labelsBuffer + intoffset2) = clusterID;
				*(offsetBuffer + *offsetEnd) = intoffset2;
				(*offsetEnd)++;
			}
		}
	}
}

// release all umats
void dbScanGPUNoise::releaseSUMats()
{
	std::vector<cv::UMat>::iterator itMatU;
	for (itMatU = neighbourVecXSU.begin(); itMatU != neighbourVecXSU.end(); ++itMatU) {
		itMatU->release();
	}
	for (itMatU = neighbourVecYSU.begin(); itMatU != neighbourVecYSU.end(); ++itMatU) {
		itMatU->release();
	}
	for (itMatU = neighbourVecPointSUB.begin(); itMatU != neighbourVecPointSUB.end(); ++itMatU) {
		itMatU->release();
	}

	neighbourVecXSU.clear();
	neighbourVecYSU.clear();
	neighbourVecPointSUB.clear();
}
