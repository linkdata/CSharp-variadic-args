#define _SCL_SECURE_NO_WARNINGS 1

#include <iostream>
#include <string> 
#include <sstream>

#define DEBUG_OUTPUT 0

#define STAR_TYPE_STRING 0x01
#define STAR_TYPE_BINARY 0x02
#define STAR_TYPE_LONG 0x03
#define STAR_TYPE_ULONG 0x04
#define STAR_TYPE_DECIMAL 0x05
#define STAR_TYPE_FLOAT 0x06
#define STAR_TYPE_DOUBLE 0x07
#define STAR_TYPE_REFERENCE 0x08

struct VarArg
{
	int64_t TypeCode;
	int64_t LongValue;
	uint64_t ULongValue;
	double DoubleValue;
	const void* PointerValue;
};

extern "C" __declspec(dllexport) uint32_t CSharpVarArgsNative(wchar_t* buf, uint32_t maxbuf, int64_t argc, const VarArg* argv)
{
	if (buf == NULL || maxbuf < 1)
		return 0;
	std::wstringstream ss;
	while (argc-- > 0)
	{
		const VarArg* arg = argv++;
		switch (arg->TypeCode)
		{
		case STAR_TYPE_LONG:
			ss << L"[LONG=" << arg->LongValue;
			break;
		case STAR_TYPE_ULONG:
			ss << L"[ULONG=" << arg->ULongValue;
			break;
		case STAR_TYPE_FLOAT:
			ss << L"[FLOAT=" << (float)arg->DoubleValue;
			break;
		case STAR_TYPE_DOUBLE:
			ss << L"[DOUBLE=" << arg->DoubleValue;
			break;
		case STAR_TYPE_STRING:
			ss << L"[STRING='" << (const wchar_t *)arg->PointerValue << L'\'';
			break;
		case STAR_TYPE_BINARY:
		{
			ss << L"[BINARY:";
			const uint8_t* ptr = static_cast<const uint8_t*>(arg->PointerValue);
			for (int64_t i = 0; i < arg->LongValue; i++)
			{
				ss << L' ' << *ptr++;
			}
			break;
		}
		case STAR_TYPE_REFERENCE:
			ss << L"[REFERENCE:" << arg->LongValue << L'@' << arg->ULongValue;
			break;
		default:
			ss << L"[Unknown TypeCode:" << arg->TypeCode;
			break;
		}
		ss << L"] ";
	}
	std::wstring ws = ss.str();
	memset(buf, 0, sizeof(wchar_t) * maxbuf);
	if (ws.size() < maxbuf)
		ws.copy(buf, ws.size());
	return ws.size();
}
