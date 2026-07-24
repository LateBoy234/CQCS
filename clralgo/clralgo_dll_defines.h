#ifndef CLRALGO_DLL_DEFINES_H
#define CLRALGO_DLL_DEFINES_H

#ifdef _MSC_VER
# if CLRALGO_LINKED_AS_SHARED_LIBRARY
#  define CLRALGO_API __declspec(dllimport)
# elif CLRALGO_CREATE_SHARED_LIBRARY
#  define CLRALGO_API __declspec(dllexport)
# endif
#elif __GNUC__ >= 4 || defined(__clang__)
# define CLRALGO_API __attribute__((visibility ("default")))
#endif // _MSC_VER

#ifndef CLRALGO_API
# define CLRALGO_API
#endif

#endif // CLRALGO_DLL_DEFINES_H
