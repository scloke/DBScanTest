#pragma once

#include "Global.h"

#ifndef UTILITY_H
#define UTILITY_H

class utility
{
public:
	utility();
	~utility();

	static cv::Mat getMat(BYTE* matstruct, BYTE* buffer);
	static std::tuple<int, int, int, int, int, int> getMatStruct(BYTE* matstruct);
	static int getIntFromBuffer(BYTE* buffer, int offset);
	static void setIntToBuffer(BYTE* buffer, int offset, int value);
	static void setIntToBuffer(BYTE* buffer, int offset, std::vector<int> values);

	// converts mat to vec
	template<typename T> static std::vector<T> matToVec(const cv::Mat& mat)
	{
		std::vector<T> returnVec = mat.reshape(0, 1);
		return returnVec;
	}

	// converts vec to mat
	template<typename T> static cv::Mat vecToMat(const std::vector<T>& vec, int width, int height, int type)
	{
		cv::Mat returnMat(height, width, type);
		memcpy(returnMat.data, vec.data(), vec.size() * sizeof(T));
		return returnMat;
	}

	// erases a value from a vector
	template<typename T> static void eraseVal(std::vector<T>& vec, T value)
	{
		if (vec.size() >= parallelLimit) {
			vec.erase(std::remove(std::execution::par_unseq, vec.begin(), vec.end(), value), vec.end());
		}
		else {
			vec.erase(std::remove(std::execution::seq, vec.begin(), vec.end(), value), vec.end());
		}
	}

	// get distinct indices
	template<typename T> static void distinct(std::vector<T>& vec)
	{
		std::sort(std::execution::par_unseq, vec.begin(), vec.end());
		vec.erase(std::unique(std::execution::par_unseq, vec.begin(), vec.end()), vec.end());
	}

private:
	static const int parallelLimit = 100;
};

#endif
