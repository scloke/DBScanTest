// ScanSegmentC.cpp : Defines the exported functions for the DLL.

#include "pch.h" // use pch.h in Visual Studio 2019

#include "ScanSegmentC.h"

ScanSegment* scan0 = NULL;
FastSuperpixel* scan1 = NULL;
OpenCVSuperpixels* scan2 = NULL;
DStutzSuperpixels* scan3 = NULL;
static const int setconcurrency = 4;

void initScan()
{
	// check thread concurrency
	int concurrentthreads = std::thread::hardware_concurrency();
	if (concurrentthreads == 0) {
		concurrentthreads = setconcurrency;
	}

	if (scan0 == NULL) {
		scan0 = new ScanSegment(concurrentthreads);
	}
	if (scan1 == NULL) {
		scan1 = new FastSuperpixel();
	}
	if (scan2 == NULL) {
		scan2 = new OpenCVSuperpixels();
	}
	if (scan3 == NULL) {
		scan3 = new DStutzSuperpixels();
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
}

int segment(BYTE* imgstruct_in, BYTE* imgbuffer_in, BYTE* imgstructlabels_out, BYTE* imgbufferlabels_out, int boundsx, int boundsy, int boundswidth, int boundsheight, int superpixels, float multiplier, bool merge, int type, int processor, int* duration)
{
	cv::Mat img_input = utility::getMat(imgstruct_in, imgbuffer_in);
	cv::Mat img_labels = utility::getMat(imgstructlabels_out, imgbufferlabels_out);
	cv::Rect bounds(boundsx, boundsy, boundswidth, boundsheight);

	int segments = 0;
	if (type == 0) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan0->segment(img_input, bounds, &img_labels, superpixels, multiplier, merge, processor);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 1) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan1->segment(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 2) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan2->segmentSEEDS(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 3) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan2->segmentLSC(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 4) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan2->segmentSLIC(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 5) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan2->segmentSLICO(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 6) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan2->segmentMSLIC(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 7) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan3->segmentFH(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 8) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan3->segmentERS(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 9) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan3->segmentCRS(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}
	else if (type == 10) {
		auto tstart = std::chrono::high_resolution_clock::now();

		segments = scan3->segmentETPS(img_input, bounds, &img_labels, superpixels, multiplier, merge);

		auto tend = std::chrono::high_resolution_clock::now();
		*duration = (int)std::chrono::duration_cast<std::chrono::microseconds>(tend - tstart).count();
	}

	return segments;
}

// SCANSEGMENT FUNCTIONS
ScanSegment::ScanSegment(int concurrentthreads)
{
	ScanSegment::concurrentthreads = concurrentthreads;
}

ScanSegment::~ScanSegment()
{
}

int ScanSegment::segment(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge, int processor)
{
	int imageWidth = mat.cols;
	int imageHeight = mat.rows;
	indexSize = bounds.height * bounds.width;
	int adjSuperpixels = (int)((float)superpixels * multiplier);
	clusterSize = (int)(1.1f * (float)(bounds.width * bounds.height) / (float)adjSuperpixels);
	int limitthreads = concurrentthreads;
	if (processor > 0) {
		limitthreads = MIN(concurrentthreads, processor);
	}

	// 1) divide bounds area into uniformly distributed rectangular segments
	int shortCount = (int)floorf(sqrtf((float)limitthreads));
	int longCount = limitthreads / shortCount;
	int horzDiv = bounds.width > bounds.height ? longCount : shortCount;
	int vertDiv = bounds.width > bounds.height ? shortCount : longCount;
	float horzLength = (float)bounds.width / (float)horzDiv;
	float vertLength = (float)bounds.height / (float)vertDiv;
	int effectivethreads = horzDiv * vertDiv;

	// create buffers and initialise
	int* labelsBuffer = static_cast<int*>(malloc(indexSize * sizeof(int)));
	int* clusterBuffer = static_cast<int*>(malloc(indexSize * sizeof(int)));
	cv::Vec3b* labBuffer = static_cast<cv::Vec3b*>(malloc(indexSize * sizeof(cv::Vec3b)));
	int neighbourLocBuffer[neighbourCount];
	std::vector<int*> offsetVec(effectivethreads);
	int offsetSize = (clusterSize + 1) * sizeof(int);
	bool offsetAllocated = true;
	for (int i = 0; i < effectivethreads; i++) {
		offsetVec[i] = static_cast<int*>(malloc(offsetSize));
		if (offsetVec[i] == NULL) {
			offsetAllocated = false;
		}
	}
	for (int i = 0; i < neighbourCount; i++) {
		neighbourLocBuffer[i] = (neighbourLoc[i].y * bounds.width) + neighbourLoc[i].x;
	}
	std::atomic<int> clusterIndex, locationIndex, clusterID;
	clusterIndex.store(0);
	locationIndex.store(0);
	clusterID.store(1);

	int clusterCount = 0;
	if (labelsBuffer != NULL && clusterBuffer != NULL && labBuffer != NULL && offsetAllocated) {
		smallClusters = indexSize / smallClustersDiv;

		// set labels to unclassified
		if (labelsMat->empty()) {
			*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
		}
		else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
			labelsMat->release();
			*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
		}
		else {
			labelsMat->setTo(NONE);
		}

		// set labels to unclassified
		std::fill(labelsBuffer, labelsBuffer + indexSize, UNCLASSIFIED);

		// 2) get array of seed rects
		std::vector<cv::Rect> seedRects(horzDiv * vertDiv);
		for (int y = 0; y < vertDiv; y++) {
			for (int x = 0; x < horzDiv; x++) {
				int xStart = bounds.x + (int)((float)x * horzLength);
				int yStart = bounds.y + (int)((float)y * vertLength);
				seedRects[(y * horzDiv) + x] = cv::Rect(xStart, yStart, (int)(x == horzDiv - 1 ? bounds.x + bounds.width - xStart : horzLength), (int)(y == vertDiv - 1 ? bounds.y + bounds.height - yStart : vertLength));
			}
		}

		// get initial rect and umat
		cv::Rect windowB(1, 1, bounds.width, bounds.height);

		// 3) initialise normalised lab values and multiplier
		cv::Mat labMat;
		cv::cvtColor(mat(bounds), labMat, cv::COLOR_BGR2Lab);
		cv::medianBlur(labMat, labMat, 3);

		// 4) get adjusted tolerance = (100 / average length (horz/vert)) x sqrt(3) [ie. euclidean lab colour distance sqrt(l2 + a2 + b2)] x tolerance100
		float adjTolerance = (200.0f / (imageWidth + imageHeight)) * sqrtf(3) * tolerance100;
		adjTolerance = adjTolerance * adjTolerance;

		// create neighbour vector
		std::vector<int> indexNeighbourVec(effectivethreads);
		std::iota(indexNeighbourVec.begin(), indexNeighbourVec.end(), 0);

		// create process vector
		std::vector<std::pair<int, int>> indexProcessVec(processthreads);
		int processDiv = indexSize / processthreads;
		int processCurrent = 0;
		for (int i = 0; i < processthreads - 1; i++) {
			indexProcessVec[i] = std::make_pair(processCurrent, processCurrent + processDiv);
			processCurrent += processDiv;
		}
		indexProcessVec[processthreads - 1] = std::make_pair(processCurrent, indexSize);

		// copy mat to buffer
		memcpy(labBuffer, labMat.data, indexSize * sizeof(cv::Vec3b));

		// start at the center of the rect, then run through the remainder
		std::for_each(std::execution::par_unseq, indexNeighbourVec.begin(), indexNeighbourVec.end(), [&](int& v) {
			cv::Rect seedRect = seedRects[v];
			cv::Size boundsSize = bounds.size();
			for (int y = seedRect.y; y < seedRect.y + seedRect.height; y++) {
				for (int x = seedRect.x; x < seedRect.x + seedRect.width; x++) {
					expandCluster(labBuffer, labelsBuffer, neighbourLocBuffer, clusterBuffer, offsetVec[v], boundsSize, cv::Point(x, y), (int)adjTolerance, &clusterIndex, &locationIndex, &clusterID);
				}
			}
		});

		cv::Mat labels(bounds.height, bounds.width, CV_32SC1);
		if (merge) {
			// get cutoff size for clusters
			std::vector<std::pair<int, int>> countVec;
			int clusterIndexSize = clusterIndex.load();
			countVec.reserve(clusterIndexSize / 2);
			for (int i = 1; i < clusterIndexSize; i += 2) {
				int count = clusterBuffer[i];
				if (count >= smallClusters) {
					int clusterID = clusterBuffer[i - 1];
					countVec.push_back(std::make_pair(clusterID, count));
				}
			}

			// sort descending
			std::sort(std::execution::par_unseq, countVec.begin(), countVec.end(), [](auto& left, auto& right) {
				return left.second > right.second;
			});

			int cutoff = MAX(smallClusters, countVec[MIN(countVec.size() - 1, adjSuperpixels - 1)].second);
			clusterCount = (int)std::count_if(countVec.begin(), countVec.end(), [&cutoff](std::pair<int, int> p) {return p.second > cutoff; });

			// change labels to 1 -> clusterCount, 0 = UNKNOWN, reuse clusterbuffer
			std::fill_n(clusterBuffer, indexSize, UNKNOWN);
			int countLimit = cutoff == -1 ? countVec.size() : clusterCount;
			for (int i = 0; i < countLimit; i++) {
				clusterBuffer[countVec[i].first] = i + 1;
			}

			std::for_each(std::execution::par_unseq, indexProcessVec.begin(), indexProcessVec.end(), [&labelsBuffer, &clusterBuffer](std::pair<int, int>& p) {
				for (int i = p.first; i < p.second; i++) {
					labelsBuffer[i] = clusterBuffer[labelsBuffer[i]];
				}
			});

			// copy in labels to mats
			memcpy(labels.data, labelsBuffer, indexSize * sizeof(int));
			cv::Mat pixelMat(bounds.height, bounds.width, CV_8UC1);
			cv::compare(labels, UNKNOWN, pixelMat, cv::CMP_EQ);

			// run watershed
			cv::Mat labelsWS = labels.clone();
			watershedEx(labMat, labelsWS);
			labelsWS.copyTo(labels, pixelMat);

			// change labels to 0 -> superpixels - 1
			cv::subtract(labels, 1, labels);

			labelsWS.release();
			pixelMat.release();
		}
		else {
			memcpy(labels.data, labelsBuffer, indexSize * sizeof(int));
		}
		labels.copyTo((*labelsMat)(bounds));
		labels.release();
		labMat.release();
	}
	else {
		// clear labels
		labelsMat->release();
		*labelsMat = cv::Mat();
	}

	// clean up
	if (labelsBuffer != NULL) {
		free(labelsBuffer);
	}
	if (clusterBuffer != NULL) {
		free(clusterBuffer);
	}
	if (labBuffer != NULL) {
		free(labBuffer);
	}
	for (int i = 0; i < effectivethreads; i++) {
		if (offsetVec[i] != NULL) {
			free(offsetVec[i]);
		}
	}

	return clusterCount;
}

// expand clusters from a point
void ScanSegment::expandCluster(cv::Vec3b* labBuffer, int* labelsBuffer, int* neighbourLocBuffer, int* clusterBuffer, int* offsetBuffer, const cv::Size& bounds, const cv::Point& point, int adjTolerance, std::atomic<int>* clusterIndex, std::atomic<int>* locationIndex, std::atomic<int>* clusterID)
{
	int pointIndex = (point.y * bounds.width) + point.x;
	if (labelsBuffer[pointIndex] == UNCLASSIFIED) {
		int offsetStart = 0;
		int offsetEnd = 0;
		int currentClusterID = clusterID->fetch_add(1);
		calculateCluster(labBuffer, labelsBuffer, neighbourLocBuffer, offsetBuffer, &offsetEnd, bounds, pointIndex, adjTolerance, currentClusterID);

		if (offsetStart == offsetEnd) {
			labelsBuffer[pointIndex] = UNKNOWN;
		}
		else {
			// set cluster id and get core point index
			labelsBuffer[pointIndex] = currentClusterID;

			while (offsetStart < offsetEnd) {
				int intoffset2 = *(offsetBuffer + offsetStart);
				offsetStart++;
				calculateCluster(labBuffer, labelsBuffer, neighbourLocBuffer, offsetBuffer, &offsetEnd, bounds, intoffset2, adjTolerance, currentClusterID);
			}

			// add origin point
			offsetBuffer[offsetEnd] = pointIndex;
			offsetEnd++;

			// store to buffer
			int currentClusterIndex = clusterIndex->fetch_add(2);
			clusterBuffer[currentClusterIndex] = currentClusterID;
			clusterBuffer[currentClusterIndex + 1] = offsetEnd;
		}
	}
}

void ScanSegment::calculateCluster(cv::Vec3b* labBuffer, int* labelsBuffer, int* neighbourLocBuffer, int* offsetBuffer, int* offsetEnd, const cv::Size& bounds, int pointIndex, int adjTolerance, int currentClusterID)
{
	for (int i = 0; i < neighbourCount; i++) {
		if (*offsetEnd < clusterSize) {
			int intoffset2 = pointIndex + neighbourLocBuffer[i];
			if (intoffset2 >= 0 && intoffset2 < indexSize && labelsBuffer[intoffset2] == UNCLASSIFIED) {
				int diff1 = (int)labBuffer[pointIndex][0] - (int)labBuffer[intoffset2][0];
				int diff2 = (int)labBuffer[pointIndex][1] - (int)labBuffer[intoffset2][1];
				int diff3 = (int)labBuffer[pointIndex][2] - (int)labBuffer[intoffset2][2];

				if ((diff1 * diff1) + (diff2 * diff2) + (diff3 * diff3) <= adjTolerance) {
					labelsBuffer[intoffset2] = currentClusterID;
					offsetBuffer[*offsetEnd] = intoffset2;
					(*offsetEnd)++;
				}
			}
		}
		else { break; }
	}
}

int ScanSegment::allocWSNodes(std::vector<ScanSegment::WSNode>& storage)
{
	int sz = (int)storage.size();
	int newsz = MAX(128, sz * 3 / 2);

	storage.resize(newsz);
	if (sz == 0)
	{
		storage[0].next = 0;
		sz = 1;
	}
	for (int i = sz; i < newsz - 1; i++)
		storage[i].next = i + 1;
	storage[newsz - 1].next = 0;
	return sz;
}

//the modified version of watershed algorithm from OpenCV
void ScanSegment::watershedEx(const cv::Mat& src, cv::Mat& dst)
{
	// https://github.com/Seaball/watershed_with_mask

	// Labels for pixels
	const int IN_QUEUE = -2; // Pixel visited
	// possible bit values = 2^8
	const int NQ = 256;

	cv::Size size = src.size();
	int channel = src.channels();
	// Vector of every created node
	std::vector<WSNode> storage;
	int free_node = 0, node;
	// Priority queue of queues of nodes
	// from high priority (0) to low priority (255)
	WSQueue q[NQ];
	// Non-empty queue with highest priority
	int active_queue;
	int i, j;
	// Color differences
	int db, dg, dr;
	int subs_tab[513];

	// MAX(a,b) = b + MAX(a-b,0)
#define ws_max(a,b) ((b) + subs_tab[(a)-(b)+NQ])
	// MIN(a,b) = a - MAX(a-b,0)
#define ws_min(a,b) ((a) - subs_tab[(a)-(b)+NQ])

	// Create a new node with offsets mofs and iofs in queue idx
#define ws_push(idx,mofs,iofs)          \
        {                                       \
    if (!free_node)                    \
    free_node = allocWSNodes(storage); \
    node = free_node;                   \
    free_node = storage[free_node].next; \
    storage[node].next = 0;             \
    storage[node].mask_ofs = mofs;      \
    storage[node].img_ofs = iofs;       \
    if (q[idx].last)                   \
    storage[q[idx].last].next = node; \
    else                                \
  q[idx].first = node;            \
  q[idx].last = node;                 \
        }

	// Get next node from queue idx
#define ws_pop(idx,mofs,iofs)           \
        {                                       \
    node = q[idx].first;                \
    q[idx].first = storage[node].next;  \
    if (!storage[node].next)           \
    q[idx].last = 0;                \
    storage[node].next = free_node;     \
    free_node = node;                   \
    mofs = storage[node].mask_ofs;      \
    iofs = storage[node].img_ofs;       \
        }

	// Get highest absolute channel difference in diff
#define c_diff(ptr1,ptr2,diff)           \
        {                                        \
    db = std::abs((ptr1)[0] - (ptr2)[0]); \
    dg = std::abs((ptr1)[1] - (ptr2)[1]); \
    dr = std::abs((ptr1)[2] - (ptr2)[2]); \
    diff = ws_max(db, dg);                \
    diff = ws_max(diff, dr);              \
    assert(0 <= diff && diff <= 255);  \
        }

	//get absolute difference in diff
#define c_gray_diff(ptr1,ptr2,diff)		\
        {									\
    diff = std::abs((ptr1)[0] - (ptr2)[0]);	\
    assert(0 <= diff&&diff <= 255);		\
        }

	CV_Assert(src.type() == CV_8UC3 || src.type() == CV_8UC1 && dst.type() == CV_32SC1);
	CV_Assert(src.size() == dst.size());

	// Current pixel in input image
	const uchar* img = src.ptr();
	// Step size to next row in input image
	int istep = int(src.step / sizeof(img[0]));

	// Current pixel in mask image
	int* mask = dst.ptr<int>();
	// Step size to next row in mask image
	int mstep = int(dst.step / sizeof(mask[0]));

	for (i = 0; i < 256; i++)
		subs_tab[i] = 0;
	for (i = 256; i <= 512; i++)
		subs_tab[i] = i - 256;

	//for (j = 0; j < size.width; j++)
	//mask[j] = mask[j + mstep*(size.height - 1)] = 0;

	// initial phase: put all the neighbor pixels of each marker to the ordered queue -
	// determine the initial boundaries of the basins
	for (i = 1; i < size.height - 1; i++) {
		img += istep; mask += mstep;
		mask[0] = mask[size.width - 1] = 0; // boundary pixels

		for (j = 1; j < size.width - 1; j++) {
			int* m = mask + j;
			if (m[0] < 0)
				m[0] = 0;
			if (m[0] == 0 && (m[-1] > 0 || m[1] > 0 || m[-mstep] > 0 || m[mstep] > 0))
			{
				// Find smallest difference to adjacent markers
				const uchar* ptr = img + j * channel;
				int idx = 256, t;
				if (m[-1] > 0) {
					if (channel == 3) {
						c_diff(ptr, ptr - channel, idx);
					}
					else {
						c_gray_diff(ptr, ptr - channel, idx);
					}
				}
				if (m[1] > 0) {
					if (channel == 3) {
						c_diff(ptr, ptr + channel, t);
					}
					else {
						c_gray_diff(ptr, ptr + channel, t);
					}
					idx = ws_min(idx, t);
				}
				if (m[-mstep] > 0) {
					if (channel == 3) {
						c_diff(ptr, ptr - istep, t);
					}
					else {
						c_gray_diff(ptr, ptr - istep, t);
					}
					idx = ws_min(idx, t);
				}
				if (m[mstep] > 0) {
					if (channel == 3) {
						c_diff(ptr, ptr + istep, t);
					}
					else {
						c_gray_diff(ptr, ptr + istep, t);
					}
					idx = ws_min(idx, t);
				}

				// Add to according queue
				assert(0 <= idx && idx <= 255);
				ws_push(idx, i * mstep + j, i * istep + j * channel);
				m[0] = IN_QUEUE;//initial unvisited
			}
		}
	}
	// find the first non-empty queue
	for (i = 0; i < NQ; i++)
		if (q[i].first)
			break;

	// if there is no markers, exit immediately
	if (i == NQ)
		return;

	active_queue = i;//first non-empty priority queue
	img = src.ptr();
	mask = dst.ptr<int>();

	// recursively fill the basins
	for (;;)
	{
		int mofs, iofs;
		int lab = 0, t;
		int* m;
		const uchar* ptr;

		// Get non-empty queue with highest priority
		// Exit condition: empty priority queue
		if (q[active_queue].first == 0)
		{
			for (i = active_queue + 1; i < NQ; i++)
				if (q[i].first)
					break;
			if (i == NQ)
			{
				std::vector<WSNode>().swap(storage);
				break;
			}
			active_queue = i;
		}

		// Get next node
		ws_pop(active_queue, mofs, iofs);
		int top = 1, bottom = 1, left = 1, right = 1;
		if (0 <= mofs && mofs < mstep)//pixel on the top
			top = 0;
		if ((mofs % mstep) == 0)//pixel in the left column
			left = 0;
		if ((mofs + 1) % mstep == 0)//pixel in the right column
			right = 0;
		if (mstep * (size.height - 1) <= mofs && mofs < mstep * size.height)//pixel on the bottom
			bottom = 0;

		// Calculate pointer to current pixel in input and marker image
		m = mask + mofs;
		ptr = img + iofs;
		int diff, temp;
		// Check surrounding pixels for labels to determine label for current pixel
		if (left) {//the left point can be visited
			t = m[-1];
			if (t > 0) {
				lab = t;
				if (channel == 3) {
					c_diff(ptr, ptr - channel, diff);
				}
				else {
					c_gray_diff(ptr, ptr - channel, diff);
				}
			}
		}
		if (right) {// Right point can be visited
			t = m[1];
			if (t > 0) {
				if (lab == 0) {//and this point didn't be labeled before
					lab = t;
					if (channel == 3) {
						c_diff(ptr, ptr + channel, diff);
					}
					else {
						c_gray_diff(ptr, ptr + channel, diff);
					}
				}
				else if (t != lab) {
					if (channel == 3) {
						c_diff(ptr, ptr + channel, temp);
					}
					else {
						c_gray_diff(ptr, ptr + channel, temp);
					}
					diff = ws_min(diff, temp);
					if (diff == temp)
						lab = t;
				}
			}
		}
		if (top) {
			t = m[-mstep]; // Top
			if (t > 0) {
				if (lab == 0) {//and this point didn't be labeled before
					lab = t;
					if (channel == 3) {
						c_diff(ptr, ptr - istep, diff);
					}
					else {
						c_gray_diff(ptr, ptr - istep, diff);
					}
				}
				else if (t != lab) {
					if (channel == 3) {
						c_diff(ptr, ptr - istep, temp);
					}
					else {
						c_gray_diff(ptr, ptr - istep, temp);
					}
					diff = ws_min(diff, temp);
					if (diff == temp)
						lab = t;
				}
			}
		}
		if (bottom) {
			t = m[mstep]; // Bottom
			if (t > 0) {
				if (lab == 0) {
					lab = t;
				}
				else if (t != lab) {
					if (channel == 3) {
						c_diff(ptr, ptr + istep, temp);
					}
					else {
						c_gray_diff(ptr, ptr + istep, temp);
					}
					diff = ws_min(diff, temp);
					if (diff == temp)
						lab = t;
				}
			}
		}
		// Set label to current pixel in marker image
		assert(lab != 0);//lab must be labeled with a nonzero number
		m[0] = lab;

		// Add adjacent, unlabeled pixels to corresponding queue
		if (left) {
			if (m[-1] == 0)//left pixel with marker 0
			{
				if (channel == 3) {
					c_diff(ptr, ptr - channel, t);
				}
				else {
					c_gray_diff(ptr, ptr - channel, t);
				}
				ws_push(t, mofs - 1, iofs - channel);
				active_queue = ws_min(active_queue, t);
				m[-1] = IN_QUEUE;
			}
		}

		if (right)
		{
			if (m[1] == 0)//right pixel with marker 0
			{
				if (channel == 3) {
					c_diff(ptr, ptr + channel, t);
				}
				else {
					c_gray_diff(ptr, ptr + channel, t);
				}
				ws_push(t, mofs + 1, iofs + channel);
				active_queue = ws_min(active_queue, t);
				m[1] = IN_QUEUE;
			}
		}

		if (top)
		{
			if (m[-mstep] == 0)//top pixel with marker 0
			{
				if (channel == 3) {
					c_diff(ptr, ptr - istep, t);
				}
				else {
					c_gray_diff(ptr, ptr - istep, t);
				}
				ws_push(t, mofs - mstep, iofs - istep);
				active_queue = ws_min(active_queue, t);
				m[-mstep] = IN_QUEUE;
			}
		}

		if (bottom) {
			if (m[mstep] == 0)//down pixel with marker 0
			{
				if (channel == 3) {
					c_diff(ptr, ptr + istep, t);
				}
				else {
					c_gray_diff(ptr, ptr + istep, t);
				}
				ws_push(t, mofs + mstep, iofs + istep);
				active_queue = ws_min(active_queue, t);
				m[mstep] = IN_QUEUE;
			}
		}
	}
}

FastSuperpixel::FastSuperpixel()
{
}

FastSuperpixel::~FastSuperpixel()
{
}

int FastSuperpixel::segment(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	// convert mat to rgb buffers
	int pixel = bounds.height * bounds.width;

	std::vector<cv::Mat> bgrVec;
	cv::split(mat(bounds), bgrVec);
	unsigned char* R = static_cast<unsigned char*>(malloc(pixel * sizeof(unsigned char)));
	unsigned char* G = static_cast<unsigned char*>(malloc(pixel * sizeof(unsigned char)));
	unsigned char* B = static_cast<unsigned char*>(malloc(pixel * sizeof(unsigned char)));
	memcpy(R, bgrVec[2].data, pixel);
	memcpy(G, bgrVec[1].data, pixel);
	memcpy(B, bgrVec[0].data, pixel);
	bgrVec[0].release();
	bgrVec[1].release();
	bgrVec[2].release();

	unsigned short* label = static_cast<unsigned short*>(calloc(pixel, sizeof(unsigned short)));

	int realnumber = 0;						// actual number of superpixels
	int post = merge ? 1 : 0;				// post-processing enabled = 1

	DBscan(R, G, B, bounds.height, bounds.width, label, (double)((float)superpixels * multiplier), realnumber, post);

	// convert labels to mat
	int* labelInt = static_cast<int*>(malloc(pixel * sizeof(int)));
	for (int i = 0; i < pixel; i++) {
		labelInt[i] = (int)label[i];
	}
	memcpy(labelsMat->data, labelInt, pixel * sizeof(int));

	free(label);
	free(labelInt);
	free(R);
	free(G);
	free(B);

	return realnumber;
}

OpenCVSuperpixels::OpenCVSuperpixels()
{
}

OpenCVSuperpixels::~OpenCVSuperpixels()
{
}

int OpenCVSuperpixels::segmentSEEDS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	int imageWidth = mat.cols;
	int imageHeight = mat.rows;
	int adjSuperpixels = (int)((float)superpixels * multiplier);

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	cv::Ptr<cv::ximgproc::SuperpixelSEEDS> seeds = cv::ximgproc::createSuperpixelSEEDS(imageWidth, imageHeight, mat.channels(), adjSuperpixels, levels, prior, histogramBins, false);

	cv::Mat labMat;
	cv::cvtColor(mat(bounds), labMat, cv::COLOR_BGR2Lab);

	seeds->iterate(labMat, iterations);
	int clusterCount = seeds->getNumberOfSuperpixels();
	cv::Mat labels;
	seeds->getLabels(labels);
	cv::add(labels, 1, labels);
	labels.copyTo((*labelsMat)(bounds));

	labels.release();
	labMat.release();
	seeds.release();

	return clusterCount;
}

int OpenCVSuperpixels::segmentLSC(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	int imageWidth = mat.cols;
	int imageHeight = mat.rows;

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	cv::Mat labMat;
	cv::cvtColor(mat(bounds), labMat, cv::COLOR_BGR2Lab);

	int regionSize = (int)sqrtf((float)(imageWidth * imageHeight) * multiplier / (float)superpixels);

	cv::Ptr<cv::ximgproc::SuperpixelLSC> lsc = cv::ximgproc::createSuperpixelLSC(labMat, regionSize);

	lsc->iterate(iterations);

	if (merge) {
		lsc->enforceLabelConnectivity();
	}

	int clusterCount = lsc->getNumberOfSuperpixels();
	cv::Mat labels;
	lsc->getLabels(labels);
	cv::add(labels, 1, labels);
	labels.copyTo((*labelsMat)(bounds));

	labels.release();
	labMat.release();

	return clusterCount;
}

int OpenCVSuperpixels::segmentSLIC(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	int imageWidth = mat.cols;
	int imageHeight = mat.rows;

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	cv::Mat labMat;
	cv::cvtColor(mat(bounds), labMat, cv::COLOR_BGR2Lab);

	int regionSize = (int)sqrtf((float)(imageWidth * imageHeight) * multiplier / (float)superpixels);

	cv::Ptr<cv::ximgproc::SuperpixelSLIC> slic = cv::ximgproc::createSuperpixelSLIC(labMat, cv::ximgproc::SLIC, regionSize);

	slic->iterate(iterations);

	if (merge) {
		slic->enforceLabelConnectivity();
	}

	int clusterCount = slic->getNumberOfSuperpixels();
	cv::Mat labels;
	slic->getLabels(labels);
	cv::add(labels, 1, labels);
	labels.copyTo((*labelsMat)(bounds));

	labels.release();
	labMat.release();

	return clusterCount;
}

int OpenCVSuperpixels::segmentSLICO(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	int imageWidth = mat.cols;
	int imageHeight = mat.rows;

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	cv::Mat labMat;
	cv::cvtColor(mat(bounds), labMat, cv::COLOR_BGR2Lab);

	int regionSize = (int)sqrtf((float)(imageWidth * imageHeight) * multiplier / (float)superpixels);

	cv::Ptr<cv::ximgproc::SuperpixelSLIC> slico = cv::ximgproc::createSuperpixelSLIC(labMat, cv::ximgproc::SLICO, regionSize);

	slico->iterate(iterations);

	if (merge) {
		slico->enforceLabelConnectivity();
	}

	int clusterCount = slico->getNumberOfSuperpixels();
	cv::Mat labels;
	slico->getLabels(labels);
	cv::add(labels, 1, labels);
	labels.copyTo((*labelsMat)(bounds));

	labels.release();
	labMat.release();

	return clusterCount;
}

int OpenCVSuperpixels::segmentMSLIC(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	int imageWidth = mat.cols;
	int imageHeight = mat.rows;

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	cv::Mat labMat;
	cv::cvtColor(mat(bounds), labMat, cv::COLOR_BGR2Lab);

	int regionSize = (int)sqrtf((float)(imageWidth * imageHeight) * multiplier / (float)superpixels);

	cv::Ptr<cv::ximgproc::SuperpixelSLIC> mslic = cv::ximgproc::createSuperpixelSLIC(labMat, cv::ximgproc::MSLIC, regionSize);

	mslic->iterate(iterations);

	if (merge) {
		mslic->enforceLabelConnectivity();
	}

	int clusterCount = mslic->getNumberOfSuperpixels();
	cv::Mat labels;
	mslic->getLabels(labels);
	cv::add(labels, 1, labels);
	labels.copyTo((*labelsMat)(bounds));

	labels.release();
	labMat.release();

	return clusterCount;
}

DStutzSuperpixels::DStutzSuperpixels()
{
}

DStutzSuperpixels::~DStutzSuperpixels()
{
}

int DStutzSuperpixels::countSuperpixels(int** labels, int rows, int cols) {
	assert(rows > 0);
	assert(cols > 0);

	int maxLabel = 0;
	for (int i = 0; i < rows; i++) {
		for (int j = 0; j < cols; j++) {
			assert(labels[i][j] >= 0);

			if (labels[i][j] > maxLabel) {
				maxLabel = labels[i][j];
			}
		}
	}

	bool* foundLabels = new bool[maxLabel + 1];
	for (int k = 0; k < maxLabel + 1; k++) {
		foundLabels[k] = false;
	}

	int count = 0;
	int label = 0;
	for (int i = 0; i < rows; i++) {
		for (int j = 0; j < cols; j++) {
			label = labels[i][j];

			if (foundLabels[label] == false) {
				foundLabels[label] = true;
				count++;
			}
		}
	}

	// cleanup
	delete[] foundLabels;

	return count;
}

int DStutzSuperpixels::computeRegionSizeFromSuperpixels(const cv::Mat& image, int superpixels) {

	return (int)(0.5f + std::sqrt(image.rows * image.cols / (float)superpixels));
}

void DStutzSuperpixels::relabelSuperpixels(cv::Mat& labels)
{
	int max_label = 0;
	for (int i = 0; i < labels.rows; i++) {
		for (int j = 0; j < labels.cols; j++) {
			if (labels.at<int>(i, j) > max_label) {
				max_label = labels.at<int>(i, j);
			}
		}
	}

	int current_label = 0;
	std::vector<int> label_correspondence(max_label + 1, -1);

	for (int i = 0; i < labels.rows; i++) {
		for (int j = 0; j < labels.cols; j++) {
			int label = labels.at<int>(i, j);

			if (label_correspondence[label] < 0) {
				label_correspondence[label] = current_label++;
			}

			labels.at<int>(i, j) = label_correspondence[label];
		}
	}
}

int DStutzSuperpixels::relabelConnectedSuperpixels(cv::Mat& labels) {
	relabelSuperpixels(labels);

	int max = 0;
	for (int i = 0; i < labels.rows; ++i) {
		for (int j = 0; j < labels.cols; ++j) {
			if (labels.at<int>(i, j) > max) {
				max = labels.at<int>(i, j);
			}
		}
	}

	ConnectedComponents cc(2 * max);

	cv::Mat components(labels.rows, labels.cols, CV_32SC1, cv::Scalar(0));
	int component_count = cc.connected<int, int, std::equal_to<int>, bool>((int*)labels.data, (int*)components.data, labels.cols,
		labels.rows, std::equal_to<int>(), false);

	for (int i = 0; i < labels.rows; i++) {
		for (int j = 0; j < labels.cols; j++) {
			labels.at<int>(i, j) = components.at<int>(i, j);
		}
	}

	// component_count would be the NEXT label index, max is the current highest!
	return component_count - max - 1;
}

void DStutzSuperpixels::computeHeightWidthLevelsFromSuperpixels(const cv::Mat& image, int superpixels, int& height, int& width, int& levels)
{
	int max_width = 20;
	int max_height = 20;
	int max_levels = 12;

	int min_difference = -1;
	levels = 1;
	width = 2;
	height = 2;

	for (int w = 2; w <= max_width; ++w) {
		for (int h = 2; h <= max_height; ++h) {
			for (int l = 1; l <= max_levels; ++l) {
				int computed_superpixels = std::floor(image.cols / (w * pow(2, l - 1)))
					* std::floor(image.rows / (h * pow(2, l - 1)));

				int difference = abs(superpixels - computed_superpixels);
				if (difference < min_difference || min_difference < 0) {
					min_difference = difference;
					levels = l;
					width = w;
					height = h;
				}
			}
		}
	}
}

void DStutzSuperpixels::computeHeightWidthFromSuperpixels(const cv::Mat& image,	int superpixels, int& height, int& width)
{
	int s = image.rows * image.cols / superpixels;
	height = 0.5f + std::sqrt(s * image.rows / (float)image.cols);
	width = 0.5f + s / (float)height;
}

int DStutzSuperpixels::segmentFH(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	float sigma = 0.0f;
	float adjThreshold = 30000.0f;
	int minimum_size = 10;

	int imageWidth = mat.cols;
	int imageHeight = mat.rows;
	int adjSuperpixels = (int)((float)superpixels * multiplier);
	float threshold = (adjThreshold / adjSuperpixels) * (adjThreshold / adjSuperpixels);

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	cv::Mat labels;
	FH_OpenCV::computeSuperpixels(mat(bounds), sigma, threshold, minimum_size, labels);

	relabelConnectedSuperpixels(labels);
	std::vector<int> labelsVec = labels.reshape(0, 1);
	utility::distinct(labelsVec);
	int clusterCount = labelsVec.size();
	labels.copyTo((*labelsMat)(bounds));

	// clean up
	labels.release();

	return clusterCount;
}

int DStutzSuperpixels::segmentERS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	double lambda = 0.5;
	double sigma = 5.0;
	int four_connected = 0;

	int imageWidth = mat.cols;
	int imageHeight = mat.rows;
	int adjSuperpixels = (int)((float)superpixels * multiplier);

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	cv::Mat labels;
	ERS_OpenCV::computeSuperpixels(mat(bounds), adjSuperpixels, lambda, sigma, four_connected, labels);

	std::vector<int> labelsVec = labels.reshape(0, 1);
	utility::distinct(labelsVec);
	int clusterCount = labelsVec.size();
	labels.copyTo((*labelsMat)(bounds));

	// clean up
	labels.release();

	return clusterCount;
}

int DStutzSuperpixels::segmentCRS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	double clique_cost = 0.3;
	double compactness = 0.045;
	int iterations = 3;
	int color_space = 1;

	int imageWidth = mat.cols;
	int imageHeight = mat.rows;
	int adjSuperpixels = (int)((float)superpixels * multiplier);

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	int region_width;
	int region_height;
	computeHeightWidthFromSuperpixels(mat(bounds), adjSuperpixels, region_height, region_width);

	cv::Mat labels;
	CRS_OpenCV::computeSuperpixels(mat(bounds), region_height, region_width, clique_cost, compactness, iterations, color_space, labels);

	std::vector<int> labelsVec = labels.reshape(0, 1);
	utility::distinct(labelsVec);
	int clusterCount = labelsVec.size();
	labels.copyTo((*labelsMat)(bounds));

	// clean up
	labels.release();

	return clusterCount;
}

int DStutzSuperpixels::segmentETPS(const cv::Mat& mat, const cv::Rect& bounds, cv::Mat* labelsMat, int superpixels, float multiplier, bool merge)
{
	double regularization_weight = 1.0;
	double length_weight = 1.0;
	double size_weight = 1.0;
	int iterations = 1;

	int imageWidth = mat.cols;
	int imageHeight = mat.rows;
	int adjSuperpixels = (int)((float)superpixels * multiplier);

	if (labelsMat->empty()) {
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else if (labelsMat->rows != imageHeight || labelsMat->cols != imageWidth || labelsMat->type() != CV_32SC1) {
		labelsMat->release();
		*labelsMat = cv::Mat(imageHeight, imageWidth, CV_32SC1, cv::Scalar(NONE));
	}
	else {
		labelsMat->setTo(NONE);
	}

	int region_size = computeRegionSizeFromSuperpixels((*labelsMat)(bounds), adjSuperpixels);
	cv::Mat labels;
	ETPS_OpenCV::computeSuperpixels(mat(bounds), region_size, regularization_weight, length_weight, size_weight, iterations, labels);

	std::vector<int> labelsVec = labels.reshape(0, 1);
	utility::distinct(labelsVec);
	int clusterCount = labelsVec.size();
	labels.copyTo((*labelsMat)(bounds));

	// clean up
	labels.release();

	return clusterCount;
}
