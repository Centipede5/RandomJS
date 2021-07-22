CXXFLAGS=-Wall  -march=native -O2 -I ~/environment/boost_1_67_0
BINDIR=bin
SRCDIR=randomjs
OBJDIR=obj
EXECUTABLE=$(BINDIR)/randomjs
LDFLAGS=-L/home/ubuntu/environment/boost_1_67_0/stage/lib -lpthread -s EXPORTED_FUNCTIONS="['_main', '_malloc','__ZN16OperatorSelectorI13UnaryOperatorN14ProgramOptions14UnaryOperatorsEE6selectER15RandomGenerator']"
# -s EXPORTED_FUNCTIONS=_main,_malloc,__ZN16OperatorSelectorI13UnaryOperatorN14ProgramOptions14UnaryOperatorsEE6selectER15RandomGenerator
SOURCES=$(wildcard $(SRCDIR)/*.c*)
OBJS=$(patsubst %.cpp,$(OBJDIR)/%.o,$(notdir $(SOURCES))) $(OBJDIR)/blake2b.o

$(EXECUTABLE): $(OBJS) | $(BINDIR)
	$(CXX) $(OBJS) $(LDFLAGS) 

$(OBJDIR)/blake2b.o: $(SRCDIR)/blake2/blake2b.c | $(OBJDIR)
	$(CXX) $(CXXFLAGS) -c $< -o $@

$(OBJDIR)/%.o: $(SRCDIR)/%.cpp | $(OBJDIR)
	$(CXX) $(CXXFLAGS) -std=c++17 -c $< -o $@
  
$(OBJDIR):
	mkdir $(OBJDIR)
  
$(BINDIR):
	mkdir $(BINDIR)

clean:
	rm -f $(EXECUTABLE) $(OBJDIR)/*.o
# /home/ubuntu/environment/boost_1_67_0