// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "LogUtil.h"

std::shared_ptr<spdlog::logger> RIOLogger;
std::once_flag OnceRIO;

class ExitCaller;
std::shared_ptr<ExitCaller> _ExitCaller;

class ExitCaller {
public:
	ExitCaller() {}

	~ExitCaller() {
		// Under VisualStudio, this must be called before main finishes to workaround a known VS issue
		//GetLogger().g("will exit RIO dll");
		spdlog::drop_all();
	}
private:
	ExitCaller(const ExitCaller&) = delete;
	ExitCaller& operator=(const ExitCaller&) = delete;
};

std::shared_ptr<spdlog::logger> GetRIOLogger() {
	std::call_once(OnceRIO, []() {
		spdlog::set_async_mode(8192);
		spdlog::set_pattern(" **** %Y-%m-%d %H:%M:%S.%e %l **** %v");
		spdlog::set_level(spdlog::level::debug);
		RIOLogger = spdlog::daily_logger_mt("file_logger", "rioDaily", 23, 59, true);
		_ExitCaller.reset(new ExitCaller);

	});
	return RIOLogger;
}

struct RIOBuf {
	INT64 BufferId;
	UINT32 Offset;
	UINT32 Length;

	std::string&& ToString() const {
		char buf[100] = { 0 };
		sprintf_s(buf, "{ BufferId = %ld, Offset=%d, Length=%d }", BufferId, Offset, Length);
		return std::move(std::string(buf));
	}
};

std::string&& ToString(const RIO_RQ& socketQueue) {
	char buf[100] = { 0 };
	sprintf_s(buf, "%ul", socketQueue);
	return std::move(std::string(buf));
}

std::string&& ToString(const PRIO_BUF& pData) {
	if (nullptr == pData) {
		return "null";
	}

	auto buf = reinterpret_cast<RIOBuf*>(pData);
	if (nullptr == buf) {
		return "null-error-cast";
	}
	return buf->ToString();
}