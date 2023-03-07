BENCHMARKS := Timepiece.Benchmarks
IMAGE := timepiece
CONTAINER := tptest
NTRIALS := 1
TIMEOUT := 30
MAXSIZE := 40
LOGDIR := logs
RESULTDIR := results
POLICIES := r lw v h ar alw av ah

.PHONY: all
all: plots

# build the docker image
.PHONY: image
image: Dockerfile
	docker build --rm -t $(IMAGE) .

# run the monolithic benchmark
$(RESULTDIR)/%-m.dat: image run_all.py
	docker run --name $(CONTAINER) $(IMAGE) python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k 4 $(MAXSIZE) --dat -- $(*F) -m
	docker cp $(CONTAINER):/timepiece/logs .
	docker cp $(CONTAINER):/timepiece/results .
	docker rm $(CONTAINER)

# run the modular benchmark
$(RESULTDIR)/%.dat: image run_all.py
	docker run --name $(CONTAINER) $(IMAGE) python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k 4 $(MAXSIZE) --dat -- $(*F)
	docker cp $(CONTAINER):/timepiece/logs .
	docker cp $(CONTAINER):/timepiece/results .
	docker rm $(CONTAINER)

# generate the plot from the benchmarks
# use the first prereq as \benchmod and the second as \benchmono
$(RESULTDIR)/%.pdf: $(RESULTDIR)/%.dat $(RESULTDIR)/%-m.dat
	pdflatex -jobname $(*F) -halt-on-error -output-directory results "\newcommand\timeout{$(TIMEOUT)}\newcommand\benchmod{$(word 1,$(^F))}\newcommand\benchmono{$(word 2,$(^F))}\input{plot.tex}"

bench:	$(addprefix $(RESULTDIR)/, $(POLICIES:=.dat)) $(addprefix $(RESULTDIR)/, $(POLICIES:=-m.dat))

plots:	$(addprefix $(RESULTDIR)/, $(POLICIES:=.pdf))

internet2: internet2.angler.json image
	docker run --name $(CONTAINER) $(IMAGE) dotnet /timepiece/publish/Timepiece.Angler

.PHONY: clean
clean:
	  rm -rf $(RESULTDIR)
	  rm -rf $(LOGDIR)
