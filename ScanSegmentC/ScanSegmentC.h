#pragma once

#ifdef SCANSEGMENTC_EXPORTS
#define SCANSEGMENT_API __declspec(dllexport)
#else
#define SCANSEGMENT_API __declspec(dllimport)
#endif

#pragma warning(push, 0)
#pragma warning (disable : 6001 26451)
#include "FastSuperpixel/DBscan.h"
#pragma warning(pop)

#include "lib_eval/connected_components.h"
#include "lib_fh/fh_opencv.h"
#include "lib_ers/ers_opencv.h"
#include "lib_crs/crs_opencv.h"
#include "lib_etps/etps_opencv.h"
#include "Global.h"
#include "Utility.h"
#include <map>

extern "C" SCANSEGMENT_API void initScan();

extern "C" SCANSEGMENT_API void exitScan();

extern "C" SCANSEGMENT_API int segment(BYTE* imgstruct_in, BYTE* imgbuffer_in, BYTE* imgstructlabels_out, BYTE* imgbufferlabels_out, int boundsx, int boundsy, int boundswidth, int boundsheight, int superpixels, float multiplier, bool merge, int type, int processor, int* duration);

#ifndef DBSCAN_H
#define DBSCAN_H

// calculate the total sum of differences - Euclidian distance (CIEDE2000)
// see http://en.wikipedia.org/wiki/Color_difference
class CIEDE
{
#define E76ADH 0.8f
public:

private:
	static inline double deg2Rad(const double deg)
	{
		return (deg * (M_PI / 180.0));
	}
};

class OpenCVSuperpixels
{
public:
	OpenCVSuperpixels();
	~OpenCVSuperpixels();

	int segmentSEEDS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);
	int segmentLSC(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);
	int segmentSLIC(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);
	int segmentSLICO(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);
	int segmentMSLIC(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);

private:
	static const int iterations = 4;
	static const int levels = 4;
	static const int prior = 2;
	static const int histogramBins = 5;
};

class DStutzSuperpixels
{
public:
	DStutzSuperpixels();
	~DStutzSuperpixels();

	int segmentFH(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);
	int segmentERS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);
	int segmentCRS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);
	int segmentETPS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);

private:
	static int countSuperpixels(int** labels, int rows, int cols);
	static int computeRegionSizeFromSuperpixels(const cv::Mat& image, int superpixels);
	static void relabelSuperpixels(cv::Mat& labels);
	static int relabelConnectedSuperpixels(cv::Mat& labels);
	static void computeHeightWidthLevelsFromSuperpixels(const cv::Mat& image, int superpixels, int& height, int& width, int& levels);
	static void computeHeightWidthFromSuperpixels(const cv::Mat& image, int superpixels, int& height, int& width);
};

// superpixel segmentation with dbScan
class ScanSegment
{
#define UNKNOWN 0
#define BORDER -1
#define UNCLASSIFIED -2
#define NONE -3

public:
	ScanSegment(int concurrentthreads);
	~ScanSegment();

	int segment(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge, int processor);

private:
	static const int neighbourCount = 8;							// number of pixel neighbours
	static const int mergeMul = 15;									// search distance multiplier for merging
	static const int minSearchNeighbours = 3;						// minimum number of neighbours to search for merging
	static const int smallClustersDiv = 10000;						// divide total pixels by this to give smallClusters
	static const int parallelLimit = 100;							// processing above this limit should be parallelised
	const float tolerance100 = 10.0f;								// colour tolerance for image size of 100x100px
	int concurrentthreads = 4;										// number of simultaneous concurrent threads
	int processthreads = 4;											// number of simultaneous process threads
	int indexSize = 0;												// size of label mat vector
	int clusterSize = 0;											// max size of clusters
	int smallClusters = 0;											// clusters below this pixel count are considered small for merging
	std::vector<cv::Point> neighbourLoc{ cv::Point(-1, -1), cv::Point(0, -1), cv::Point(1, -1), cv::Point(-1, 0), cv::Point(1, 0), cv::Point(-1, 1), cv::Point(0, 1), cv::Point(1, 1) };

	struct WSNode
	{
		int next;
		int mask_ofs;
		int img_ofs;
	};

	// Queue for WSNodes
	struct WSQueue
	{
		WSQueue() { first = last = 0; }
		int first, last;
	};

	void expandCluster(cv::Vec3b* labBuffer, int* labelsBuffer, int* neighbourLocBuffer, int* clusterBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int adjTolerance, std::atomic<int>* clusterIndex, std::atomic<int>* locationIndex, std::atomic<int>* clusterID);
	void calculateCluster(cv::Vec3b* labBuffer, int* labelsBuffer, int* neighbourLocBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int pointIndex, int adjTolerance, int currentClusterID);
	static int allocWSNodes(std::vector<WSNode>& storage);
	void watershedEx(const cv::Mat& src, cv::Mat& dst);
};

// superpixel segmentation with dbScan
class FastSuperpixel
{
public:
	FastSuperpixel();
	~FastSuperpixel();

	int segment(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge);

private:

};

#endif
