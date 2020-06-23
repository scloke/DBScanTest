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
		std::vector<T> returnVec;
		if (mat.isContinuous()) {
			returnVec = mat.reshape(0, 1);
		}
		else {
			cv::Mat matCont = mat.clone();
			returnVec = matCont.reshape(0, 1);
			matCont.release();
		}
		return returnVec;
	}

private:
};

#endif
