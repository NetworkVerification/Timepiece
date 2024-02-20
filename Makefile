# Makefile for running Timepiece benchmarks
# Note: This is used to run Timepiece's internal (C#) benchmarks, as described in the PLDI2023 paper
# and artifact evaluation, and implemented in the Timepiece.Benchmarks application.
# To run Timepiece on network configurations using Timepiece.Angler, we recommend following the advice in README.md instead.
# The docker image name.
IMAGE := timepiece
# The docker container built from the image.
CONTAINER := tptest
# Number of trials to perform
NTRIALS := 1
# Timeout in seconds
TIMEOUT := 60
# Minimum size fat-tree (in terms of pods)
MINSIZE := 4
# Maximum size fat-tree (in terms of pods)
MAXSIZE := 40
RUNALLCMD := python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k $(MINSIZE) $(MAXSIZE) --dat
LOGDIR := logs
RESULTDIR := results
# The policies we wish to test.
POLICIES := reachSymbolic lengthSymbolic valleySymbolic hijackSymbolic allReachSymbolic allLengthSymbolic allValleySymbolic allHijackSymbolic
# The Angler configuration file for Internet2
INTERNET2 := INTERNET2.angler.json

.PHONY: all
all: plots

# build the docker image
.PHONY: image
image: Dockerfile run_all.py
	docker build --rm -t $(IMAGE) .

# run the monolithic benchmark
$(RESULTDIR)/%-m.dat: | image $(RESULTDIR)
	docker run --rm --name $(CONTAINER) \
		--mount type=bind,source=$(RESULTDIR),target=/timepiece/$(RESULTDIR) \
		--mount type=bind,source=$(LOGDIR),target=/timepiece/$(LOGDIR) \
		$(IMAGE) $(RUNALLCMD) -- $(*F) -m

# run the modular benchmark
$(RESULTDIR)/%.dat: | image $(RESULTDIR)
	docker run --rm --name $(CONTAINER) \
		--mount type=bind,source=$(RESULTDIR),target=/timepiece/$(RESULTDIR) \
		--mount type=bind,source=$(LOGDIR),target=/timepiece/$(LOGDIR) \
		$(IMAGE) $(RUNALLCMD) -- $(*F)

# generate the plot from the benchmarks
# use the first prereq as \benchmod and the second as \benchmono
$(RESULTDIR)/%.pdf: $(RESULTDIR)/%.dat $(RESULTDIR)/%-m.dat | $(RESULTDIR)
	pdflatex -jobname $(*F) -halt-on-error -output-directory $(RESULTDIR) \
	"\newcommand\timeout{$(TIMEOUT)}\newcommand\benchmod{$(word 1,$(^F))}\newcommand\benchmono{$(word 2,$(^F))}\input{plot.tex}"

bench:	$(addprefix $(RESULTDIR)/, $(POLICIES:=.dat)) $(addprefix $(RESULTDIR)/, $(POLICIES:=-m.dat))

plots:	$(addprefix $(RESULTDIR)/, $(POLICIES:=.pdf))

wan: $(LOGDIR)/internet2.txt $(LOGDIR)/internet2-m.txt

$(LOGDIR)/internet2.txt: $(INTERNET2) | $(LOGDIR) image
	docker run --rm --name $(CONTAINER) \
		--mount type=bind,source=$(LOGDIR),target=/timepiece/$(LOGDIR) \
		--mount type=bind,source=$(INTERNET2),target=/timepiece/$(INTERNET2) \
		$(IMAGE) sh -c 'timeout $(TIMEOUT) dotnet /timepiece/publish/Timepiece.Angler.dll $< | tee $@'

$(LOGDIR)/internet2-m.txt: $(INTERNET2) | $(LOGDIR) image
	docker run --rm --name $(CONTAINER) \
		--mount type=bind,source=$(INTERNET2),target=/timepiece/$(INTERNET2) \
		$(IMAGE) sh -c 'timeout $(TIMEOUT) dotnet /timepiece/publish/Timepiece.Angler.dll $< -m | tee $@'

$(LOGDIR):
	mkdir -p $(LOGDIR)

$(RESULTDIR):
	mkdir -p $(RESULTDIR)

.PHONY: clean
clean:
	  rm -rf $(RESULTDIR)
	  rm -rf $(LOGDIR)

# relocate old logs and results
.PHONY: archive
archive:
	[ ! -d $(LOGDIR) ] || mv $(LOGDIR) "$(LOGDIR).$$(date +%FT%H-%M-%S)"
	[ ! -d $(RESULTDIR) ] || mv $(RESULTDIR) "$(RESULTDIR).$$(date +%FT%H-%M-%S)"
