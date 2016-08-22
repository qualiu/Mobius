// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <Windows.h>
#include <mswsockdef.h>
#include "spdlog/spdlog.h"

std::shared_ptr<spdlog::logger> GetRIOLogger();

std::string&& ToString(const RIO_RQ& socketQueue);

std::string&& ToString(const PRIO_BUF& pData);
