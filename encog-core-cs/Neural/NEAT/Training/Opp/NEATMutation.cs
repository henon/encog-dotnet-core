﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Encog.ML.EA.Opp.Selection;
using Encog.ML.EA.Train;
using Encog.MathUtil.Randomize;
using Encog.ML.EA.Genome;

namespace Encog.Neural.NEAT.Training.Opp
{
    /// <summary>
    /// This class represents a NEAT mutation. NEAT supports several different types
    /// of mutations. This class provides common utility needed by any sort of a NEAT
    /// mutation. This class is abstract and cannot be used by itself.
    /// 
    /// -----------------------------------------------------------------------------
    /// http://www.cs.ucf.edu/~kstanley/ Encog's NEAT implementation was drawn from
    /// the following three Journal Articles. For more complete BibTeX sources, see
    /// NEATNetwork.java.
    /// 
    /// Evolving Neural Networks Through Augmenting Topologies
    /// 
    /// Generating Large-Scale Neural Networks Through Discovering Geometric
    /// Regularities
    /// 
    /// Automatic feature selection in neuroevolution
    /// </summary>
    public abstract class NEATMutation : IEvolutionaryOperator
    {
        /// <summary>
        /// The trainer that owns this class.
        /// </summary>
        private IEvolutionaryAlgorithm owner;

        /// <summary>
        /// Choose a random neuron.
        /// </summary>
        /// <param name="target">The target genome. Should the input and bias neurons be
        /// included.</param>
        /// <param name="choosingFrom">True if we are chosing from all neurons, false if we exclude
        /// the input and bias.</param>
        /// <returns>The random neuron.</returns>
        public NEATNeuronGene ChooseRandomNeuron(NEATGenome target,
                bool choosingFrom)
        {
            int start;

            if (choosingFrom)
            {
                start = 0;
            }
            else
            {
                start = target.InputCount + 1;
            }

            // if this network will not "cycle" then output neurons cannot be source
            // neurons
            if (!choosingFrom)
            {
                int ac = ((NEATPopulation)target.Population)
                        .ActivationCycles;
                if (ac == 1)
                {
                    start += target.OutputCount;
                }
            }

            int end = target.NeuronsChromosome.Count - 1;

            // no neurons to pick!
            if (start > end)
            {
                return null;
            }

            int neuronPos = RangeRandomizer.RandomInt(start, end);
            NEATNeuronGene neuronGene = target.NeuronsChromosome[neuronPos];
            return neuronGene;

        }

        /// <summary>
        /// Create a link between two neuron id's. Create or find any necessary
        /// innovation records.
        /// </summary>
        /// <param name="target">The target genome.</param>
        /// <param name="neuron1ID">The id of the source neuron.</param>
        /// <param name="neuron2ID">The id of the target neuron.</param>
        /// <param name="weight">The weight of this new link.</param>
        public void CreateLink(NEATGenome target, long neuron1ID,
                long neuron2ID, double weight)
        {

            // first, does this link exist? (and if so, hopefully disabled,
            // otherwise we have a problem)
            foreach (NEATLinkGene linkGene in target.LinksChromosome)
            {
                if ((linkGene.FromNeuronID == neuron1ID)
                        && (linkGene.ToNeuronID == neuron2ID))
                {
                    // bring the link back, at the new weight
                    linkGene.Enabled = true;
                    linkGene.Weight = weight;
                    return;
                }
            }

            // check to see if this innovation has already been tried
            NEATInnovation innovation = ((NEATPopulation)target
                    .Population).Innovations.FindInnovation(neuron1ID,
                    neuron2ID);

            // now create this link
            NEATLinkGene lg = new NEATLinkGene(neuron1ID, neuron2ID,
                    true, innovation.InnovationID, weight);
            target.LinksChromosome.Add(lg);
        }

        /// <summary>
        /// Get the specified neuron's index.
        /// </summary>
        /// <param name="target">The neuron id to check for.</param>
        /// <param name="neuronID">The neuron id.</param>
        /// <returns>The index.</returns>
        public int GetElementPos(NEATGenome target, long neuronID)
        {

            for (int i = 0; i < target.NeuronsChromosome.Count; i++)
            {
                NEATNeuronGene neuronGene = target.NeuronsChromosome[i];
                if (neuronGene.Id == neuronID)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// The owner.
        /// </summary>
        public IEvolutionaryAlgorithm Owner
        {
            get
            {
                return this.owner;
            }
        }

        /// <inheritdoc/>
        public void Init(IEvolutionaryAlgorithm theOwner)
        {
            this.owner = theOwner;
        }

        /// <summary>
        /// Determine if this is a duplicate link. 
        /// </summary>
        /// <param name="target">The target genome.</param>
        /// <param name="fromNeuronID">The from neuron id.</param>
        /// <param name="toNeuronID">The to neuron id.</param>
        /// <returns>True if this is a duplicate link.</returns>
        public bool IsDuplicateLink(NEATGenome target,
                long fromNeuronID, long toNeuronID)
        {
            foreach (NEATLinkGene linkGene in target.LinksChromosome)
            {
                if ((linkGene.Enabled)
                        && (linkGene.FromNeuronID == fromNeuronID)
                        && (linkGene.ToNeuronID == toNeuronID))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a neuron is still needed. If all links to/from a neuron
        /// have been removed, then the neuron is no longer needed. 
        /// </summary>
        /// <param name="target">The target genome.</param>
        /// <param name="neuronID">The neuron id to check for.</param>
        /// <returns>Returns true, if the neuron is still needed.</returns>
        public bool IsNeuronNeeded(NEATGenome target, long neuronID)
        {

            // do not remove bias or input neurons or output
            foreach (NEATNeuronGene gene in target.NeuronsChromosome)
            {
                if (gene.Id == neuronID)
                {
                    NEATNeuronGene neuron = gene;
                    if ((neuron.NeuronType == NEATNeuronType.Input)
                            || (neuron.NeuronType == NEATNeuronType.Bias)
                            || (neuron.NeuronType == NEATNeuronType.Output))
                    {
                        return true;
                    }
                }
            }

            // Now check to see if the neuron is used in any links
            foreach (NEATLinkGene gene in target.LinksChromosome)
            {
                NEATLinkGene linkGene = gene;
                if (linkGene.FromNeuronID == neuronID)
                {
                    return true;
                }
                if (linkGene.ToNeuronID == neuronID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Obtain the NEATGenome that we will mutate. NEAT mutates the genomes in
        /// place. So the parent and child genome must be the same literal object.
        /// Throw an exception, if this is not the case. 
        /// </summary>
        /// <param name="parents">The parents.</param>
        /// <param name="parentIndex">The parent index.</param>
        /// <param name="offspring">The offspring.</param>
        /// <param name="offspringIndex">The offspring index.</param>
        /// <returns>The genome that we will mutate.</returns>
        public NEATGenome ObtainGenome(IGenome[] parents,
                int parentIndex, IGenome[] offspring,
                int offspringIndex)
        {
            offspring[offspringIndex] = this.Owner.Population
                    .GenomeFactory.Factor(parents[0]);
            return (NEATGenome)offspring[offspringIndex];
        }

        /// <summary>
        /// Returns 1, as NEAT mutations only produce one child.
        /// </summary>
        public int OffspringProduced
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Returns 1, as mutations typically are asexual and only require a
        /// single parent.
        /// </summary>
        public int ParentsNeeded
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Remove the specified neuron.
        /// </summary>
        /// <param name="target">The target genome.</param>
        /// <param name="neuronID">The neuron to remove.</param>
        public void RemoveNeuron(NEATGenome target, long neuronID)
        {
            foreach (NEATNeuronGene gene in target.NeuronsChromosome)
            {
                if (gene.Id == neuronID)
                {
                    target.NeuronsChromosome.Remove(gene);
                    return;
                }
            }
        }


        /// <inheritdoc/>
        public abstract void PerformOperation(EncogRandom rnd, IGenome[] parents, int parentIndex, 
            IGenome[] offspring, int offspringIndex);
    }
}