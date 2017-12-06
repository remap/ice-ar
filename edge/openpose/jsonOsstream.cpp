// 
// jsonOsstream.cpp
//
// Copyright (c) 2017. UCLA. All rights reserved
// Author: Peter Gusev
//

#include "jsonOsstream.hpp"

#include <openpose/headers.hpp>

using namespace op;

namespace op_ice
{
    void enterAndTab(std::stringstream& osstream, const bool humanReadable, const long long bracesCounter,
                     const long long bracketsCounter)
    {
        try
        {
            if (humanReadable)
            {
                osstream << "\n";
                for (auto i = 0ll ; i < bracesCounter + bracketsCounter ; i++)
                    osstream << "\t";
            }
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }

    JsonOsstream::JsonOsstream(const bool humanReadable) :
        mHumanReadable{humanReadable},
        mBracesCounter{0},
        mBracketsCounter{0}
    {}

    JsonOsstream::~JsonOsstream()
    {
        try
        {
            enterAndTab(mOsstream, mHumanReadable, mBracesCounter, mBracketsCounter);

            if (mBracesCounter != 0 || mBracketsCounter != 0)
            {
                std::string errorMessage = "Json file wronly generated";
                if (mBracesCounter != 0)
                    errorMessage += ", number \"{\" != number \"}\": " + std::to_string(mBracesCounter) + ".";
                else if (mBracketsCounter != 0)
                    errorMessage += ", number \"[\" != number \"]\": " + std::to_string(mBracketsCounter) + ".";
                else
                    errorMessage += ".";
                error(errorMessage, __LINE__, __FUNCTION__, __FILE__);
            }
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }

    void JsonOsstream::objectOpen()
    {
        try
        {
            mBracesCounter++;
            mOsstream << "{";
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }

    void JsonOsstream::objectClose()
    {
        try
        {
            mBracesCounter--;
            enterAndTab(mOsstream, mHumanReadable, mBracesCounter, mBracketsCounter);
            mOsstream << "}";
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }

    void JsonOsstream::arrayOpen()
    {
        try
        {
            mBracketsCounter++;
            mOsstream << "[";
            enterAndTab(mOsstream, mHumanReadable, mBracesCounter, mBracketsCounter);
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }

    void JsonOsstream::arrayClose()
    {
        try
        {
            mBracketsCounter--;
            enterAndTab(mOsstream, mHumanReadable, mBracesCounter, mBracketsCounter);
            mOsstream << "]";
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }

    void JsonOsstream::key(const std::string& string)
    {
        try
        {
            enterAndTab(mOsstream, mHumanReadable, mBracesCounter, mBracketsCounter);
            mOsstream << "\"" + string + "\":";
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }

    void JsonOsstream::enter()
    {
        try
        {
            enterAndTab(mOsstream, mHumanReadable, mBracesCounter, mBracketsCounter);
        }
        catch (const std::exception& e)
        {
            error(e.what(), __LINE__, __FUNCTION__, __FILE__);
        }
    }
}
