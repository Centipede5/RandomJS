/*
(c) 2018 tevador <tevador@gmail.com>

This file is part of RandomJS.

RandomJS is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

RandomJS is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with RandomJS.  If not, see<http://www.gnu.org/licenses/>.
*/

#pragma once

#include "Expression.h"

class Literal : public Expression {
public:
	static Literal Zero;
	Literal(const char* value) : value(value) {}
	template<typename T>
	Literal(T val) {
		StringBuilder sb;
		sb << val;
		value = sb.str().data();
	}

protected:
	virtual void writeTo(std::ostream& os) const {
		os << value;
	}
private:
	const char* value;
};