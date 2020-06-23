// Utility.cpp : Defines the utility functions for the DLL.

#include "pch.h" // use pch.h in Visual Studio 2019

#include "Utility.h"

// UTILITY FUNCTIONS
utility::utility()
{
}

utility::~utility()
{
}

cv::Mat utility::getMat(BYTE* matstruct, BYTE* buffer)
{
	std::tuple<int, int, int, int, int, int> returnStruct = getMatStruct(matstruct);
	int width = std::get<0>(returnStruct);
	int height = std::get<1>(returnStruct);
	int type = std::get<4>(returnStruct);
	int step = std::get<5>(returnStruct);

	if (width == 0 || height == 0) {
		return cv::Mat();
	}
	else {
		cv::Mat mat(height, width, type, buffer, step);
		return mat;
	}
}

std::tuple<int, int, int, int, int, int> utility::getMatStruct(BYTE* matstruct)
{
	int width = getIntFromBuffer(matstruct, 0);
	int height = getIntFromBuffer(matstruct, 4);
	int channels = getIntFromBuffer(matstruct, 8);
	int length = getIntFromBuffer(matstruct, 12);
	int basetype = getIntFromBuffer(matstruct, 16);

	int type = basetype + ((channels - 1) * 8);
	if (length == 0) {
		return std::make_tuple(0, 0, 0, 0, 0, 0);
	}
	else {
		int step = length / height;
		return std::make_tuple(width, height, channels, length, type, step);
	}
}

// gets int from buffer location
int utility::getIntFromBuffer(BYTE* buffer, int offset)
{
	int returnInt = *(int*)(buffer + offset);
	return returnInt;
}

// sets int to buffer location
void utility::setIntToBuffer(BYTE* buffer, int offset, int value)
{
	BYTE* intbuffer = reinterpret_cast<BYTE*>(&value);
	memcpy(buffer + offset, intbuffer, sizeof(int));
}

// sets int vector to buffer location
void utility::setIntToBuffer(BYTE* buffer, int offset, std::vector<int> values)
{
	memcpy(buffer + offset, values.data(), sizeof(int) * values.size());
}

