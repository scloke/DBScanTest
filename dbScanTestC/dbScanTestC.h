#pragma once

#ifdef DBSCANTESTC_EXPORTS
#define DBSCAN_API __declspec(dllexport)
#else
#define DBSCAN_API __declspec(dllimport)
#endif

#pragma warning(push, 0)
#pragma warning (disable : 26495 28182)
#include "concurrentqueue.h"
#include "robin_hood.h"
#pragma warning(pop)

#include "Global.h"
#include "Utility.h"
#include <atomic>
#include <execution>
#include <unordered_set>

extern "C" DBSCAN_API void initScan(int maxwidth, int maxheight);

extern "C" DBSCAN_API void exitScan();

extern "C" DBSCAN_API int dbScan(BYTE* imgstruct_in, BYTE* imgbuffer_in, BYTE* imgstructlabels_out, BYTE* imgbufferlabels_out, int boundsx, int boundsy, int boundswidth, int boundsheight, int scantype, float toleranceValue, int minCluster, int* duration);

#ifndef DBSCAN_H
#define DBSCAN_H

// calculate the total sum of differences - Euclidian distance (CIEDE2000)
// see http://en.wikipedia.org/wiki/Color_difference
class CIEDE
{
#define E76ADH 0.8f
public:
	static float getE76Byte(cv::Vec3b labB, cv::Vec3b plabB);
	static float getCIEDE2000(cv::Vec3b labB, cv::Vec3b plabB, double k_L = 1.0, double k_C = 1.0, double k_H = 1.0);
	static void normaliseLab(const cv::UMat & labU, std::vector<cv::UMat> * diffFloatVec, float borderinit);
	static cv::UMat getMultiplier(const cv::UMat& lumU, const cv::Rect& ROI);
	static void compareNormalisedE76UMat(const std::vector<cv::UMat>& diffFloatVec, const cv::UMat& multiplier, const cv::Rect& ROI1, const cv::Rect& ROI2, cv::UMat* returnUMat);
	static void normaliseLab(const cv::Mat& lab, std::vector<cv::Mat>* diffFloatVec, float borderinit);
	static cv::Mat getMultiplier(const cv::Mat& lum, const cv::Rect& ROI);
	static cv::UMat getMultiplier(const cv::UMat& lumU);
	static void compareNormalisedE76UMat(const std::vector<cv::Mat>& diffFloatVec, const cv::Mat& multiplier, const cv::Rect& ROI1, const cv::Rect& ROI2, cv::Mat* returnMat);

private:
	static inline double deg2Rad(const double deg)
	{
		return (deg * (M_PI / 180.0));
	}
};

// standard UMAT parallelisation
class dbScanGPU
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanGPU();
	~dbScanGPU();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int neighbourCount = 8;							// number of pixel neighbours
	static const int clusterIncrements = 1000;						// increment step for cluster vectors
	int indexSize = 0;												// size of neighbour mat vector
	std::vector<cv::Point> neighbourLoc{ cv::Point(-1, -1), cv::Point(0, -1), cv::Point(1, -1), cv::Point(-1, 0), cv::Point(1, 0), cv::Point(-1, 1), cv::Point(0, 1), cv::Point(1, 1) };
	std::vector<cv::UMat> neighbourVecXSU, neighbourVecYSU, neighbourVecPointSUB;

	void scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID);
	int expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID);
	void releaseSUMats();
};

// UMAT parallelisation with pre-initialised neighbours maxwidth & maxheight
class dbScanGPUInit
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanGPUInit(int maxwidth, int maxheight);
	~dbScanGPUInit();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int neighbourCount = 8;							// number of pixel neighbours
	static const int clusterIncrements = 1000;						// increment step for cluster vectors
	int indexSize = 0;												// size of neighbour mat vector
	int maxwidth, maxheight;										// maximum preset width and height
	std::vector<cv::Point> neighbourLoc{ cv::Point(-1, -1), cv::Point(0, -1), cv::Point(1, -1), cv::Point(-1, 0), cv::Point(1, 0), cv::Point(-1, 1), cv::Point(0, 1), cv::Point(1, 1) };
	std::vector<cv::UMat> neighbourVecPointSUBPreset = std::vector<cv::UMat>(2);

	void scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID);
	int expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID);
};

// UMAT parallelisation with parallelised cluster search
class dbScanGPUPar
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanGPUPar(unsigned concurrentthreads);
	dbScanGPUPar();
	~dbScanGPUPar();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int neighbourCount = 8;							// number of pixel neighbours
	static const int clusterIncrements = 1000;						// increment step for cluster vectors
	unsigned concurrentthreads;										// thread concurrency
	int indexSize = 0;												// size of neighbour mat vector
	std::vector<cv::Point> neighbourLoc{ cv::Point(-1, -1), cv::Point(0, -1), cv::Point(1, -1), cv::Point(-1, 0), cv::Point(1, 0), cv::Point(-1, 1), cv::Point(0, 1), cv::Point(1, 1) };
	std::vector<cv::UMat> neighbourVecXSU, neighbourVecYSU, neighbourVecPointSUB;
	BYTE* bitMap;													// bit mapping for thread processing

	void scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const std::vector<int>* indexCountVec, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID);
	int expandCluster2(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const std::vector<int>* indexCountVec, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID);
	int calculateCluster(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const cv::Size& bounds, const cv::Point& point, int clusterID);
	int calculateCluster(int* neighbourBuffer, int* labelsBuffer, moodycamel::ConcurrentQueue<int>* offsetQueue, const cv::Size& bounds, int intoffset, int clusterID);
	void releaseSUMats();
};

// standard MAT parallelisation
class dbScanCPU
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanCPU();
	~dbScanCPU();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int neighbourCount = 8;							// number of pixel neighbours
	static const int clusterIncrements = 1000;						// increment step for cluster vectors
	int indexSize = 0;												// size of neighbour mat vector
	std::vector<cv::Point> neighbourLoc{ cv::Point(-1, -1), cv::Point(0, -1), cv::Point(1, -1), cv::Point(-1, 0), cv::Point(1, 0), cv::Point(-1, 1), cv::Point(0, 1), cv::Point(1, 1) };
	std::vector<cv::Mat> neighbourVecXS, neighbourVecYS, neighbourVecPointSB;

	void scan2(cv::Mat* labels, const cv::Mat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID);
	int expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID);
	void releaseSMats();
};

// MAT parallelisation with pre-initialised neighbours maxwidth & maxheight
class dbScanCPUInit
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanCPUInit(int maxwidth, int maxheight);
	~dbScanCPUInit();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int neighbourCount = 8;							// number of pixel neighbours
	static const int clusterIncrements = 1000;						// increment step for cluster vectors
	int indexSize = 0;												// size of neighbour mat vector
	int maxwidth, maxheight;										// maximum preset width and height
	std::vector<cv::Point> neighbourLoc{ cv::Point(-1, -1), cv::Point(0, -1), cv::Point(1, -1), cv::Point(-1, 0), cv::Point(1, 0), cv::Point(-1, 1), cv::Point(0, 1), cv::Point(1, 1) };
	std::vector<cv::Mat> neighbourVecPointSBPreset = std::vector<cv::Mat>(2);

	void scan2(cv::Mat* labels, const cv::Mat& labMat, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID);
	int expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID);
};

// standard dbScan with modified E27 difference
class dbScanOriginal
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanOriginal();
	~dbScanOriginal();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int clusterIncrements = 1000;						// increment step for cluster vectors

	void scan2(cv::Mat* labels, const cv::Mat& labMat, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID, float toleranceValue);
	int expandCluster2(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, int* count, int clusterID, float toleranceValue);
	std::vector<cv::Point> calculateCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, float toleranceValue);
	std::vector<cv::Point> getNeighbours(const cv::Rect& bounds, const cv::Point& point);
};

// standard dbScan with CIEDE2000 difference
class dbScanCIEDE2000
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanCIEDE2000();
	~dbScanCIEDE2000();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int clusterIncrements = 1000;						// increment step for cluster vectors

	void scan2(cv::Mat* labels, const cv::Mat& labMat, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID, float toleranceValue);
	int expandCluster2(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, int* count, int clusterID, float toleranceValue);
	std::vector<cv::Point> calculateCluster(cv::Mat* labels, const cv::Mat& pixels, const cv::Mat& multiplier, const cv::Point& point, float toleranceValue);
	std::vector<cv::Point> getNeighbours(const cv::Rect& bounds, const cv::Point& point);
};

class dbScanGPUNoise
{
#define UNKNOWN 0
#define UNCLASSIFIED -1
#define NOISE -2
#define NONE -3
#define SUCCESS 0
#define FAILURE -4
#define USHRT_MAX1 (USHRT_MAX - 1)

public:
	dbScanGPUNoise();
	~dbScanGPUNoise();

	int scan(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, float toleranceValue, int minCluster, int* duration);

private:
	static const int neighbourCount = 8;							// number of pixel neighbours
	static const int clusterIncrements = 1000;						// increment step for cluster vectors
	int indexSize = 0;												// size of neighbour mat vector
	std::vector<cv::Point> neighbourLoc{ cv::Point(-1, -1), cv::Point(0, -1), cv::Point(1, -1), cv::Point(-1, 0), cv::Point(1, 0), cv::Point(-1, 1), cv::Point(0, 1), cv::Point(1, 1) };
	std::vector<cv::UMat> neighbourVecXSU, neighbourVecYSU, neighbourVecPointSUB;

	void scan2(cv::Mat* labels, const cv::UMat& labMatU, cv::Point point, int* clusterID, float toleranceValue, int minCluster);
	void expandCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, robin_hood::unordered_flat_map<int, int>* clusterDict, int* clusterCapacity, int* clusterID);
	int expandCluster2(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int* count, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, const cv::Point& point, int clusterID);
	void calculateCluster(int* neighbourBuffer, int* labelsBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int intoffset, int clusterID);
	void releaseSUMats();
};

#endif
