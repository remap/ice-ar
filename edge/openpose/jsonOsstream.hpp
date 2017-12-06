// 
// jsonOsstream.hpp
//
// Copyright (c) 2017. UCLA. All rights reserved
// Author: Peter Gusev
//

#ifndef __jsonosstream_h__
#define __jsonosstream_h__

#include <string>
#include <openpose/core/common.hpp>

namespace op_ice
{
    class JsonOsstream
    {
    public:
        explicit JsonOsstream(const bool humanReadable = true);

        ~JsonOsstream();

        void objectOpen();

        void objectClose();

        void arrayOpen();

        void arrayClose();

        void key(const std::string& string);

        template <typename T>
        inline void plainText(const T& value)
        {
            mOsstream << value;
        }

        inline void comma()
        {
            mOsstream << ",";
        }

        void enter();

        inline std::string toString()
        {
            return mOsstream.str();
        }

    private:
        const bool mHumanReadable;
        long long mBracesCounter;
        long long mBracketsCounter;
        std::stringstream mOsstream;

        DELETE_COPY(JsonOsstream);
    };
}

#endif
