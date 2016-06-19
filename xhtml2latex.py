import sys

def iskey(str):
	return len(str) > 0 and str[0] != '-'

def isvalue(str):
	return not iskey(str)

source_path, dest_path = sys.argv[1:3]

pos = 3
args = {}
while pos < len(sys.argv):
	key = sys.argv[pos]
	if iskey(key):
		key = key[1:]
		value = sys.argv[pos+1] if pos + 1 < len(sys.argv)  else ''
		if isvalue(value):
			args[key] = value 
			pos += 1
		else:
			args[key] = ''

for key, default_value in (('root', './'), ('webroot', ''), ('babel', 'english'), ('scale-factor', '0.4')):
	if not key in args:
		args[key] = default_value

replacements = {}

def transform(line):
	return str([substitutions[char] if char in substitutions else char for char in line]

source = open(source_path, encoding='utf-8')
dest = open(dest_path, 'w', encoding='utf-8')

for line in source.readlines():
	line = line.strip('\n')
	dest
	