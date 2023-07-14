IMAGE := timepiece
CONTAINER := tptest
NTRIALS := 1
TIMEOUT := 60
MINSIZE := 4
MAXSIZE := 40
RUNALLCMD := python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k $(MINSIZE) $(MAXSIZE) --dat
LOGDIR := logs
RESULTDIR := results
ANGLERDIR := angler
# set to 1 if you want to force the repo to clone angler when INTERNET2.angler.json is present
FORCE_CLONE := 1
POLICIES := reach lengthWeak valley hijack allReach allLengthWeak allValley allHijack
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

$(LOGDIR)/internet2.txt: $(ANGLERDIR)/$(INTERNET2) | $(LOGDIR) image
	docker run --rm --name $(CONTAINER) \
		--mount type=bind,source=$(LOGDIR),target=/timepiece/$(LOGDIR) \
		--mount type=bind,source=$(ANGLERDIR)/$(INTERNET2),target=/timepiece/$(ANGLERDIR)/$(INTERNET2) \
		$(IMAGE) sh -c 'timeout $(TIMEOUT) dotnet /timepiece/publish/Timepiece.Angler.dll $< | tee $@'

$(LOGDIR)/internet2-m.txt: $(ANGLERDIR)/$(INTERNET2) | $(LOGDIR) image
	docker run --rm --name $(CONTAINER) \
		--mount type=bind,source=$(ANGLERDIR)/$(INTERNET2),target=/timepiece/$(ANGLERDIR)/$(INTERNET2) \
		$(IMAGE) sh -c 'timeout $(TIMEOUT) dotnet /timepiece/publish/Timepiece.Angler.dll $< -m | tee $@'

$(ANGLERDIR)/$(INTERNET2):
	sh $(ANGLERDIR)/internet2.sh

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
