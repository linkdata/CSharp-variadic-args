#define _SCL_SECURE_NO_WARNINGS 1

#include <iostream>
#include <string> 
#include <sstream>

#define DEBUG_OUTPUT 0

#define TYPE_STRING 0x01
#define TYPE_BINARY 0x02
#define TYPE_LONG 0x03
#define TYPE_ULONG 0x04
#define TYPE_FLOAT 0x06
#define TYPE_DOUBLE 0x07
#define TYPE_REFERENCE 0x08

struct VarArg
{
	int64_t TypeCode;
	int64_t LongValue;
	double DoubleValue;
	const void* PointerValue;
	uint64_t Userdata; // Not interpreted
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
		case TYPE_LONG:
			ss << L"[LONG=" << arg->LongValue;
			break;
		case TYPE_ULONG:
			ss << L"[ULONG=" << (uint64_t)arg->LongValue;
			break;
		case TYPE_FLOAT:
			ss << L"[FLOAT=" << (float)arg->DoubleValue;
			break;
		case TYPE_DOUBLE:
			ss << L"[DOUBLE=" << arg->DoubleValue;
			break;
		case TYPE_STRING:
			ss << L"[STRING='" << (const wchar_t *)arg->PointerValue << L'\'';
			break;
		case TYPE_BINARY:
		{
			ss << L"[BINARY:";
			const uint8_t* ptr = static_cast<const uint8_t*>(arg->PointerValue);
			for (int64_t i = 0; i < arg->LongValue; i++)
			{
				ss << L' ' << *ptr++;
			}
			break;
		}
		case TYPE_REFERENCE:
			ss << L"[REFERENCE:" << arg->LongValue << L'@' << (ptrdiff_t)arg->PointerValue;
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
